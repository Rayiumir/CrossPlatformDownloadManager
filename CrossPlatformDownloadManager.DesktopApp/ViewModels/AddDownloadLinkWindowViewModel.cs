using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Threading;
using CrossPlatformDownloadManager.Data.Models;
using CrossPlatformDownloadManager.Data.Services.DownloadFileService;
using CrossPlatformDownloadManager.Data.UnitOfWork;
using CrossPlatformDownloadManager.DesktopApp.Views;
using CrossPlatformDownloadManager.Utils;
using CrossPlatformDownloadManager.Utils.Enums;
using ReactiveUI;

namespace CrossPlatformDownloadManager.DesktopApp.ViewModels;

public class AddDownloadLinkWindowViewModel : ViewModelBase
{
    #region Private Fields

    private int? _addedDownloadFileId;

    #endregion

    #region Properties

    private string? _url;

    public string? Url
    {
        get => _url;
        set => this.RaiseAndSetIfChanged(ref _url, value?.Trim());
    }

    private ObservableCollection<Category> _categories = [];

    public ObservableCollection<Category> Categories
    {
        get => _categories;
        set => this.RaiseAndSetIfChanged(ref _categories, value);
    }

    private Category? _selectedCategory;

    public Category? SelectedCategory
    {
        get => _selectedCategory;
        set => this.RaiseAndSetIfChanged(ref _selectedCategory, value);
    }

    private string? _fileName;

    public string? FileName
    {
        get => _fileName;
        set => this.RaiseAndSetIfChanged(ref _fileName, value);
    }

    private string? _description;

    public string? Description
    {
        get => _description;
        set => this.RaiseAndSetIfChanged(ref _description, value);
    }

    private string? _fileTypeIcon;

    public string? FileTypeIcon
    {
        get => _fileTypeIcon;
        set => this.RaiseAndSetIfChanged(ref _fileTypeIcon, value);
    }

    private double _fileSize;

    public double FileSize
    {
        get => _fileSize;
        set => this.RaiseAndSetIfChanged(ref _fileSize, value);
    }

    private bool _isLoadingUrl = false;

    public bool IsLoadingUrl
    {
        get => _isLoadingUrl;
        set => this.RaiseAndSetIfChanged(ref _isLoadingUrl, value);
    }

    private ObservableCollection<DownloadQueue> _queues = [];

    public ObservableCollection<DownloadQueue> Queues
    {
        get => _queues;
        set => this.RaiseAndSetIfChanged(ref _queues, value);
    }

    private DownloadQueue? _selectedQueue;

    public DownloadQueue? SelectedQueue
    {
        get => _selectedQueue;
        set => this.RaiseAndSetIfChanged(ref _selectedQueue, value);
    }

    private bool _rememberMyChoice;

    public bool RememberMyChoice
    {
        get => _rememberMyChoice;
        set => this.RaiseAndSetIfChanged(ref _rememberMyChoice, value);
    }

    private bool _startQueue;

    public bool StartQueue
    {
        get => _startQueue;
        set => this.RaiseAndSetIfChanged(ref _startQueue, value);
    }

    private bool _defaultQueueIsExist;

    public bool DefaultQueueIsExist
    {
        get => _defaultQueueIsExist;
        set => this.RaiseAndSetIfChanged(ref _defaultQueueIsExist, value);
    }

    #endregion

    #region Commands

    public ICommand AddNewCategoryCommand { get; }

    public ICommand AddNewQueueCommand { get; }

    public ICommand AddFileToQueueCommand { get; }

    public ICommand AddToDefaultQueueCommand { get; }

    public ICommand StartDownloadCommand { get; }

    #endregion

    public AddDownloadLinkWindowViewModel(IUnitOfWork unitOfWork, IDownloadFileService downloadFileService) : base(
        unitOfWork, downloadFileService)
    {
        Categories = GetCategoriesAsync().Result;
        Queues = GetQueuesAsync().Result;
        SelectedQueue = Queues.FirstOrDefault();

        AddNewCategoryCommand = ReactiveCommand.Create<Window?>(AddNewCategory);
        AddNewQueueCommand = ReactiveCommand.Create<Window?>(AddNewQueue);
        AddFileToQueueCommand = ReactiveCommand.Create<Window?>(AddFileToQueue);
        AddToDefaultQueueCommand = ReactiveCommand.Create<Window?>(AddToDefaultQueue);
        StartDownloadCommand = ReactiveCommand.Create<Window?>(StartDownload);
    }

    private async void StartDownload(Window? owner)
    {
        // TODO: Show message box
        try
        {
            if (owner == null || !ValidateDownloadFile())
                return;

            var result = AddDownloadFileAsync(null).Result;
            if (!result)
                return;

            var downloadFile =
                DownloadFileService.DownloadFiles.FirstOrDefault(df => df.Id == _addedDownloadFileId);
            if (downloadFile == null)
                return;

            Dispatcher.UIThread.InvokeAsync(() =>
            {
                var vm = new DownloadWindowViewModel(UnitOfWork, DownloadFileService, downloadFile);
                var window = new DownloadWindow { DataContext = vm };
                window.Show();
            });

            owner.Close(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async void AddNewCategory(Window? owner)
    {
        try
        {
            if (owner == null)
                return;

            var vm = new AddNewCategoryWindowViewModel(UnitOfWork, DownloadFileService);
            var window = new AddNewCategoryWindow { DataContext = vm };
            var result = await window.ShowDialog<bool>(owner);
            if (!result)
                return;

            Categories = await GetCategoriesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async void AddNewQueue(Window? owner)
    {
        try
        {
            if (owner == null)
                return;

            var vm = new AddNewQueueWindowViewModel(UnitOfWork, DownloadFileService);
            var window = new AddNewQueueWindow { DataContext = vm };
            var result = await window.ShowDialog<bool>(owner);
            if (!result)
                return;

            Queues = await GetQueuesAsync();
            SelectedQueue = Queues.LastOrDefault();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async void AddFileToQueue(Window? owner)
    {
        try
        {
            if (owner == null)
                return;

            if (!ValidateDownloadFile())
                return;

            // TODO: Show message box
            if (SelectedQueue == null)
                return;

            var downloadQueue = await UnitOfWork.DownloadQueueRepository
                .GetAsync(where: dq => dq.Id == SelectedQueue.Id);

            if (downloadQueue == null)
                return;

            var result = await AddDownloadFileAsync(downloadQueue);
            if (!result)
                return;

            if (RememberMyChoice)
            {
                var downloadQueues = (await UnitOfWork.DownloadQueueRepository
                        .GetAllAsync(where: dq => dq.IsDefault))
                    .Select(dq =>
                    {
                        dq.IsDefault = false;
                        return dq;
                    })
                    .ToList();

                await UnitOfWork.DownloadQueueRepository.UpdateAllAsync(downloadQueues);
                await UnitOfWork.SaveAsync();

                downloadQueue.IsDefault = true;
                await UnitOfWork.DownloadQueueRepository.UpdateAsync(downloadQueue);
                await UnitOfWork.SaveAsync();
            }

            // TODO: If user choose Start Queue, then start it

            owner.Close(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async void AddToDefaultQueue(Window? owner)
    {
        try
        {
            if (owner == null)
                return;

            var defaultDownloadQueue = await UnitOfWork.DownloadQueueRepository
                .GetAsync(where: dq => dq.IsDefault);

            if (defaultDownloadQueue == null)
            {
                DefaultQueueIsExist = false;
                return;
            }

            DefaultQueueIsExist = true;

            var result = await AddDownloadFileAsync(defaultDownloadQueue);
            if (!result)
                return;

            owner.Close(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    private async Task<bool> AddDownloadFileAsync(DownloadQueue? downloadQueue)
    {
        List<DownloadFile>? downloadFilesForSelectedQueue = null;
        if (downloadQueue != null)
        {
            downloadFilesForSelectedQueue = await UnitOfWork.DownloadFileRepository
                .GetAllAsync(where: df => df.DownloadQueueId == downloadQueue.Id);
        }

        var ext = Path.GetExtension(FileName!);
        var fileExtensions = await UnitOfWork.CategoryFileExtensionRepository
            .GetAllAsync(where: fe => fe.Extension.ToLower() == ext.ToLower(),
                includeProperties: ["Category.CategorySaveDirectory"]);

        var category = fileExtensions.FirstOrDefault(fe => fe.Category != null && !fe.Category.IsDefault)?.Category
                       ?? fileExtensions.FirstOrDefault()?.Category;

        if (category?.CategorySaveDirectory == null)
            return false;

        var downloadFile = new DownloadFile
        {
            Url = Url!,
            FileName = FileName!,
            DownloadQueueId = downloadQueue?.Id,
            Size = FileSize,
            Description = Description,
            Status = DownloadStatus.None,
            LastTryDate = null,
            DateAdded = DateTime.Now,
            QueuePriority = downloadFilesForSelectedQueue != null
                ? (downloadFilesForSelectedQueue.Max(df => df.QueuePriority) ?? 0) + 1
                : null,
            CategoryId = category.Id,
            IsPaused = false,
            SaveLocation = category.CategorySaveDirectory.SaveDirectory,
        };

        await DownloadFileService.AddFileAsync(downloadFile);
        _addedDownloadFileId = downloadFile.Id;
        return true;
    }

    private bool ValidateDownloadFile()
    {
        // TODO: Show message to user
        var result = false;

        if (Url.IsNullOrEmpty() || !Url.CheckUrlValidation())
            return result;

        if (SelectedCategory == null)
            return result;

        if (FileName.IsNullOrEmpty())
            return result;

        if (!FileName.HasFileExtension())
            return result;

        result = true;
        return result;
    }

    private async Task<ObservableCollection<DownloadQueue>> GetQueuesAsync()
    {
        try
        {
            var queues = await UnitOfWork.DownloadQueueRepository.GetAllAsync();
            return queues.ToObservableCollection();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new ObservableCollection<DownloadQueue>();
        }
    }

    private async Task<ObservableCollection<Category>> GetCategoriesAsync()
    {
        try
        {
            var categories = await UnitOfWork.CategoryRepository.GetAllAsync();
            categories.Insert(0, new Category { Title = Constants.GeneralCategoryTitle });
            return categories.ToObservableCollection();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return new ObservableCollection<Category>();
        }
    }

    public async Task GetUrlInfoAsync()
    {
        IsLoadingUrl = true;

        try
        {
            if (!Url.CheckUrlValidation())
            {
                IsLoadingUrl = false;
                return;
            }

            var downloadFileWithSameUrl = await UnitOfWork.DownloadFileRepository
                .GetAsync(where: df => df.Url == Url);

            if (downloadFileWithSameUrl != null)
            {
                // TODO: Show message box
                IsLoadingUrl = false;
                return;
            }

            var httpClient = new HttpClient();
            string fileName = string.Empty;
            double fileSize = 0;

            // Send a HEAD request to get the headers only
            using var request = new HttpRequestMessage(HttpMethod.Head, Url);
            using var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Failed to retrieve URL: {response.StatusCode}");

            // Check if the Content-Type indicates a file
            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType != null && (contentType.StartsWith("application/") || contentType.StartsWith("image/") ||
                                        contentType.StartsWith("video/") || contentType.StartsWith("audio/") ||
                                        contentType == "text/plain"))
            {
                if (response.Content.Headers.ContentDisposition != null)
                    fileName = response.Content.Headers.ContentDisposition.FileName?.Trim('\"') ?? string.Empty;

                // Fallback to using the URL to guess the file name if Content-Disposition is not present
                if (fileName.IsNullOrEmpty())
                {
                    var uri = new Uri(Url!);
                    fileName = Path.GetFileName(uri.LocalPath);
                }

                // Get the content length
                fileSize = response.Content.Headers.ContentLength ?? 0;
            }

            // Set file name, file size, file icon and category
            FileName = fileName;
            FileSize = fileSize;

            // find category item by file extension
            var ext = Path.GetExtension(FileName);

            var defaultCategories = await UnitOfWork.CategoryRepository
                .GetAllAsync(where: c => !c.IsDefault, includeProperties: ["FileExtensions"]);

            CategoryFileExtension? fileExtension = null;
            var defaultCategory = defaultCategories
                .FirstOrDefault(c => c.FileExtensions
                    .Any(fe => fe.Extension.ToLower() == ext.ToLower()));

            if (defaultCategory != null)
            {
                fileExtension = defaultCategory.FileExtensions
                    .FirstOrDefault(fe => fe.Extension.ToLower() == ext.ToLower());
            }
            else
            {
                fileExtension = await UnitOfWork.CategoryFileExtensionRepository
                    .GetAsync(where: fe => fe.Extension.ToLower() == ext.ToLower(),
                        includeProperties: ["Category"]);
            }

            if (fileExtension != null)
            {
                var category = defaultCategory ?? fileExtension.Category;

                if (category != null)
                {
                    FileTypeIcon = category.Icon;
                    SelectedCategory = Categories.FirstOrDefault(c => c.Id == category.Id);
                }
                else
                {
                    SelectedCategory =
                        Categories.FirstOrDefault(c =>
                            c.Title.Equals(Constants.GeneralCategoryTitle, StringComparison.OrdinalIgnoreCase));
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

        IsLoadingUrl = false;
    }
}