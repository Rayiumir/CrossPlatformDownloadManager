using System.Collections.ObjectModel;
using AutoMapper;
using Avalonia.Controls;
using Avalonia.Threading;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.UnitOfWork;
using CrossPlatformDownloadManager.Data.ViewModels;
using CrossPlatformDownloadManager.Data.ViewModels.CustomEventArgs;
using CrossPlatformDownloadManager.Utils;
using Downloader;
using PropertyChanged;

namespace CrossPlatformDownloadManager.Data.Services.DownloadFileService;

[AddINotifyPropertyChangedInterface]
public class DownloadFileService : IDownloadFileService
{
    #region Private Fields

    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    private readonly Dictionary<int, DownloadService> _downloadServices;
    private readonly Dictionary<int, DownloadConfiguration> _downloadConfigurations;
    private readonly Dictionary<int, Window> _downloadFileWindows;
    private readonly Dictionary<int, bool> _windowClosingStates;

    #endregion

    #region Events

    public event EventHandler? DataChanged;

    #endregion

    #region Properties

    public ObservableCollection<DownloadFileViewModel> DownloadFiles { get; }

    #endregion

    public DownloadFileService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;

        DownloadFiles = [];

        _downloadServices = [];
        _downloadConfigurations = [];
        _downloadFileWindows = [];
        _windowClosingStates = [];
    }

    public async Task LoadDownloadFilesAsync()
    {
        var downloadFiles = await _unitOfWork.DownloadFileRepository
            .GetAllAsync(includeProperties: ["Category.FileExtensions", "DownloadQueue"]);

        var primaryKeys = downloadFiles
            .Select(df => df.Id)
            .ToList();

        var exceptDownloadFiles = DownloadFiles
            .Where(df => !primaryKeys.Contains(df.Id))
            .ToList();

        foreach (var downloadFile in exceptDownloadFiles)
            await DeleteDownloadFileAsync(downloadFile, alsoDeleteFile: true, reloadData: false);

        foreach (var downloadFile in downloadFiles)
        {
            var oldDownloadFile = DownloadFiles.FirstOrDefault(df => df.Id == downloadFile.Id);
            var vm = _mapper.Map<DownloadFileViewModel>(downloadFile);
            if (oldDownloadFile != null)
                UpdateDownloadFileViewModel(oldDownloadFile, vm);
            else
                DownloadFiles.Add(vm);
        }

        DataChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task AddDownloadFileAsync(DownloadFile downloadFile)
    {
        await _unitOfWork.DownloadFileRepository.AddAsync(downloadFile);
        await _unitOfWork.SaveAsync();
        await LoadDownloadFilesAsync();
    }

    public async Task UpdateDownloadFileAsync(DownloadFile downloadFile)
    {
        await _unitOfWork.DownloadFileRepository.UpdateAsync(downloadFile);
        await _unitOfWork.SaveAsync();
        await LoadDownloadFilesAsync();
    }

    public async Task UpdateDownloadFileAsync(DownloadFileViewModel viewModel)
    {
        var downloadFile = await _unitOfWork.DownloadFileRepository.GetAsync(where: df => df.Id == viewModel.Id);
        if (downloadFile == null)
            return;

        downloadFile.Status = viewModel.Status;
        downloadFile.LastTryDate = viewModel.LastTryDate;
        downloadFile.DownloadProgress = viewModel.DownloadProgress ?? 0;
        downloadFile.ElapsedTime = viewModel.ElapsedTime;
        downloadFile.TimeLeft = viewModel.TimeLeft;
        downloadFile.TransferRate = viewModel.TransferRate;
        downloadFile.DownloadPackage = viewModel.DownloadPackage;

        await UpdateDownloadFileAsync(downloadFile);
    }

    public async Task UpdateDownloadFilesAsync(List<DownloadFile> downloadFiles)
    {
        await _unitOfWork.DownloadFileRepository.UpdateAllAsync(downloadFiles);
        await _unitOfWork.SaveAsync();
        await LoadDownloadFilesAsync();
    }

    public async Task StartDownloadFileAsync(DownloadFileViewModel? downloadFile, Window? window)
    {
        if (downloadFile == null || window == null)
            return;

        window.Tag = downloadFile.Id;
        window.Closing += WindowOnClosing;

        var downloadConfiguration = new DownloadConfiguration
        {
            ChunkCount = 8,
            MaximumBytesPerSecond = 64 * 1024,
            ParallelDownload = true,
        };

        var downloadService = new DownloadService(downloadConfiguration);

        _downloadServices.Add(downloadFile.Id, downloadService);
        _downloadConfigurations.Add(downloadFile.Id, downloadConfiguration);
        _downloadFileWindows.Add(downloadFile.Id, window);
        _windowClosingStates.Add(downloadFile.Id, false);

        downloadFile.DownloadFinished += DownloadFileOnDownloadFinished;
        await downloadFile.StartDownloadFileAsync(downloadService, downloadConfiguration, _unitOfWork);
    }

    private void DownloadFileOnDownloadFinished(object? sender, DownloadFileEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == e.Id);
            if (downloadFile == null)
                return;

            downloadFile.DownloadFinished -= DownloadFileOnDownloadFinished;

            var window = _downloadFileWindows.FirstOrDefault(dw => dw.Key == e.Id).Value;
            if (window == null)
                return;

            RemoveDownloadOptions(downloadFile).GetAwaiter();
            window.Closing -= WindowOnClosing;
            window.Close();
        });
    }

    private async void WindowOnClosing(object? sender, WindowClosingEventArgs e)
    {
        // TODO: Show message box
        try
        {
            var window = sender as Window;
            if (window == null)
                return;

            var tag = window.Tag?.ToString();
            if (tag.IsNullOrEmpty() || !int.TryParse(tag, out var id))
                return;

            var downloadFile = DownloadFiles.FirstOrDefault(df => df.Id == id);
            if (downloadFile == null)
                return;

            var state = _windowClosingStates.FirstOrDefault(wcs => wcs.Key == downloadFile.Id).Value;
            if (state)
                return;

            _windowClosingStates.Remove(downloadFile.Id);
            _windowClosingStates.Add(downloadFile.Id, true);

            await StopDownloadFileAsync(downloadFile);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    public async Task StopDownloadFileAsync(DownloadFileViewModel? downloadFile, bool closeWindow = false)
    {
        if (downloadFile == null)
            return;

        var downloadService = _downloadServices.FirstOrDefault(ds => ds.Key == downloadFile.Id).Value;
        var window = _downloadFileWindows.FirstOrDefault(dw => dw.Key == downloadFile.Id).Value;
        if (downloadService == null || window == null)
            return;

        await downloadFile.StopDownloadFileAsync(downloadService);
        await RemoveDownloadOptions(downloadFile);

        window.Closing -= WindowOnClosing;

        if (closeWindow)
            window.Close();
    }

    public void ResumeDownloadFile(DownloadFileViewModel? downloadFile)
    {
        if (downloadFile == null)
            return;

        var downloadService = _downloadServices.FirstOrDefault(ds => ds.Key == downloadFile.Id).Value;
        if (downloadService == null)
            return;

        downloadFile.ResumeDownloadFile(downloadService);
    }

    public void PauseDownloadFile(DownloadFileViewModel? downloadFile)
    {
        if (downloadFile == null)
            return;

        var downloadService = _downloadServices.FirstOrDefault(ds => ds.Key == downloadFile.Id).Value;
        if (downloadService == null)
            return;

        downloadFile.PauseDownloadFile(downloadService);
    }

    public void LimitDownloadFileSpeed(DownloadFileViewModel? downloadFile, long speed)
    {
        if (downloadFile == null)
            return;

        var downloadConfiguration = _downloadConfigurations.FirstOrDefault(dc => dc.Key == downloadFile.Id).Value;
        if (downloadConfiguration == null)
            return;

        downloadConfiguration.MaximumBytesPerSecond = speed;
    }

    public async Task DeleteDownloadFileAsync(DownloadFileViewModel? downloadFile, bool alsoDeleteFile,
        bool reloadData = true)
    {
        if (downloadFile == null)
            return;

        var downloadFileInDb = await _unitOfWork.DownloadFileRepository
            .GetAsync(where: df => df.Id == downloadFile.Id);

        if (downloadFile.IsDownloading)
            await StopDownloadFileAsync(downloadFile, true);

        var shouldReturn = false;
        if (downloadFileInDb == null)
        {
            DownloadFiles.Remove(downloadFile);
            shouldReturn = true;
        }

        if (alsoDeleteFile)
        {
            var saveLocation = downloadFileInDb?.SaveLocation ?? downloadFile.SaveLocation ?? string.Empty;
            var fileName = downloadFileInDb?.FileName ?? downloadFile.FileName ?? string.Empty;

            if (!saveLocation.IsNullOrEmpty() && !fileName.IsNullOrEmpty())
            {
                var filePath = Path.Combine(saveLocation, fileName);
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
        }

        if (shouldReturn)
            return;

        _unitOfWork.DownloadFileRepository.Delete(downloadFileInDb);
        await _unitOfWork.SaveAsync();

        if (reloadData)
            await LoadDownloadFilesAsync();
    }

    #region Helpers

    private void UpdateDownloadFileViewModel(DownloadFileViewModel? oldDownloadFile,
        DownloadFileViewModel? newDownloadFile)
    {
        if (oldDownloadFile == null || newDownloadFile == null)
            return;

        var properties = newDownloadFile
            .GetType()
            .GetProperties()
            .Where(pi => !pi.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) && pi.CanWrite)
            .ToList();

        foreach (var property in properties)
        {
            var value = property.GetValue(newDownloadFile);
            property.SetValue(oldDownloadFile, value);
        }
    }

    private async Task RemoveDownloadOptions(DownloadFileViewModel downloadFile)
    {
        _downloadConfigurations.Remove(downloadFile.Id);
        _downloadServices.Remove(downloadFile.Id);
        _downloadFileWindows.Remove(downloadFile.Id);
        _windowClosingStates.Remove(downloadFile.Id);

        await UpdateDownloadFileAsync(downloadFile);
    }

    #endregion
}