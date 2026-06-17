using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BomApp.Application.Interfaces;
using BomApp.Application.Interfaces.Repositories;
using BomApp.Shared.Contracts;
using BomApp.UI.Services;

namespace BomApp.UI.ViewModels.ProductDestruction;

public partial class ProductDestructionViewModel : ViewModelBase
{
    private readonly IProductDestructionService _service;
    private readonly IErpItemRepository _erpItemRepository;
    private readonly IDialogService _dialogService;
    private bool _hasLoadedInitialDocuments;

    public Func<Task<ProductDestructionPictureEditModel?>>? ShowPicturePicker { get; set; }
    public Func<Task<ErpItemDto?>>? ShowProductSearchDialog { get; set; }

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _docNoSearch = string.Empty;
    [ObservableProperty] private DateTimeOffset? _dateFrom;
    [ObservableProperty] private DateTimeOffset? _dateTo;
    [ObservableProperty] private ProductDestructionDto? _selectedDocument;
    [ObservableProperty] private int _pageNumber = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalCount;
    [ObservableProperty] private int _totalPages = 1;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private bool _isExistingDocument;
    [ObservableProperty] private DateTimeOffset _docDate = DateTimeOffset.Now;
    [ObservableProperty] private string _docNo = string.Empty;
    [ObservableProperty] private string _remark = string.Empty;
    [ObservableProperty] private string _whCode = string.Empty;
    [ObservableProperty] private string _shelfCode = string.Empty;

    public ObservableCollection<ProductDestructionDto> Documents { get; } = new();
    public ObservableCollection<ProductDestructionPictureEditModel> Pictures { get; } = new();
    public ObservableCollection<ProductDestructionDetailEditModel> Details { get; } = new();
    public IReadOnlyList<int> PageSizeOptions { get; } = [10, 20, 50, 100];

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    public bool HasDocuments => Documents.Count > 0;
    public bool CanGoPrevious => PageNumber > 1 && !IsLoading;
    public bool CanGoNext => PageNumber < TotalPages && !IsLoading;
    public bool IsDocNoEditable => !IsExistingDocument;
    public DateTime? CalendarDocDate
    {
        get => DocDate.Date;
        set
        {
            if (value is null)
                return;

            DocDate = new DateTimeOffset(value.Value.Date, DocDate.Offset);
        }
    }

    public string PageSummary => $"หน้า {PageNumber} / {TotalPages} ({TotalCount} รายการ)";
    public string EditorTitle => IsExistingDocument ? "แก้ไขรายการของเสีย" : "เพิ่มรายการของเสีย";

    public ProductDestructionViewModel(
        IProductDestructionService service,
        IErpItemRepository erpItemRepository,
        IDialogService dialogService)
    {
        _service = service;
        _erpItemRepository = erpItemRepository;
        _dialogService = dialogService;
    }

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

    partial void OnDocDateChanged(DateTimeOffset value) => OnPropertyChanged(nameof(CalendarDocDate));

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(CanGoNext));
    }

    partial void OnIsExistingDocumentChanged(bool value)
    {
        OnPropertyChanged(nameof(IsDocNoEditable));
        OnPropertyChanged(nameof(EditorTitle));
    }

    partial void OnPageNumberChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(PageSummary));
    }

    partial void OnPageSizeChanged(int value)
    {
        PageNumber = 1;
        _ = SearchAsync();
    }

    partial void OnTotalCountChanged(int value) => OnPropertyChanged(nameof(PageSummary));

    partial void OnTotalPagesChanged(int value)
    {
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(PageSummary));
    }

    [RelayCommand]
    private async Task LoadInitialAsync()
    {
        if (_hasLoadedInitialDocuments)
            return;

        _hasLoadedInitialDocuments = true;
        await SearchAsync();
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        PageNumber = 1;
        await LoadPageAsync();
    }

    private async Task LoadPageAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var dateFrom = DateFrom.HasValue ? DateOnly.FromDateTime(DateFrom.Value.DateTime) : (DateOnly?)null;
            var dateTo = DateTo.HasValue ? DateOnly.FromDateTime(DateTo.Value.DateTime) : (DateOnly?)null;

            var result = await _service.GetDocumentsPageAsync(new ProductDestructionListQuery(
                dateFrom,
                dateTo,
                string.IsNullOrWhiteSpace(DocNoSearch) ? null : DocNoSearch.Trim(),
                PageNumber,
                PageSize));

            Documents.Clear();
            SelectedDocument = null;

            if (result.IsSuccess)
            {
                var page = result.Value!;
                TotalCount = page.TotalCount;
                TotalPages = page.TotalPages;
                PageNumber = Math.Min(page.PageNumber, TotalPages);
                PageSize = page.PageSize;

                foreach (var document in page.Items)
                    Documents.Add(document);

                OnPropertyChanged(nameof(HasDocuments));
            }
            else
            {
                ErrorMessage = result.Error ?? "ไม่สามารถโหลดรายการของเสียได้";
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SelectDocumentAsync(ProductDestructionDto document)
    {
        var result = await _service.GetDocumentByDocNoAsync(document.DocNo);
        if (!result.IsSuccess || result.Value is null)
        {
            ErrorMessage = result.Error ?? "ไม่สามารถโหลดรายละเอียดรายการของเสียได้";
            return;
        }

        SelectedDocument = result.Value;
        LoadEditor(result.Value);
    }

    [RelayCommand]
    private void NewDocument()
    {
        SelectedDocument = null;
        IsExistingDocument = false;
        IsEditing = true;
        DocDate = DateTimeOffset.Now;
        DocNo = GenerateDocNo();
        Remark = string.Empty;
        WhCode = string.Empty;
        ShelfCode = string.Empty;
        Pictures.Clear();
        Details.Clear();
        AddBlankDetail();
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            NormalizeLineNumbers();
            var date = DateOnly.FromDateTime(DocDate.DateTime);
            var details = Details
                .Where(d => !string.IsNullOrWhiteSpace(d.ItemCode))
                .Select(d => new CreateProductDestructionDetailCommand(
                    d.ItemCode,
                    d.Qty,
                    d.UnitCode,
                    d.WhCode,
                    d.ShelfCode,
                    d.LineNumber))
                .ToList();
            var pictures = Pictures
                .Select(p => new CreateProductDestructionPictureCommand(
                    p.ImageGuid,
                    p.ImageFile,
                    p.LineNumber))
                .ToList();

            var result = IsExistingDocument
                ? await _service.UpdateAsync(
                    DocNo,
                    new UpdateProductDestructionCommand(date, WhCode, ShelfCode, Remark, pictures, details))
                : await _service.CreateAsync(
                    new CreateProductDestructionCommand(DocNo, date, WhCode, ShelfCode, Remark, pictures, details));

            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "ไม่สามารถบันทึกรายการของเสียได้";
                return;
            }

            IsEditing = false;
            await LoadPageAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddPictureAsync()
    {
        if (ShowPicturePicker is null)
            return;

        var picture = await ShowPicturePicker();
        if (picture is null)
            return;

        picture.LineNumber = (short)(Pictures.Count + 1);
        Pictures.Add(picture);
    }

    [RelayCommand]
    private async Task DeleteDocumentAsync(ProductDestructionDto document)
    {
        var confirmed = await _dialogService.ConfirmAsync(
            "Confirm delete",
            $"Delete product destruction document '{document.DocNo}'?");
        if (!confirmed)
            return;

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var result = await _service.DeleteAsync(document.DocNo);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "Unable to delete product destruction document.";
                return;
            }

            Documents.Remove(document);
            if (SelectedDocument?.DocNo == document.DocNo)
            {
                SelectedDocument = null;
                IsEditing = false;
                IsExistingDocument = false;
                Pictures.Clear();
                Details.Clear();
            }

            OnPropertyChanged(nameof(HasDocuments));
            TotalCount = Math.Max(0, TotalCount - 1);
            if (Documents.Count == 0 && PageNumber > 1)
                PageNumber--;

            await LoadPageAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void RemovePicture(ProductDestructionPictureEditModel picture)
    {
        Pictures.Remove(picture);
        NormalizeLineNumbers();
    }

    [RelayCommand]
    private async Task AddItemAsync()
    {
        if (ShowProductSearchDialog is null)
        {
            AddBlankDetail();
            return;
        }

        var item = await ShowProductSearchDialog();
        if (item is null)
            return;

        var units = await _erpItemRepository.GetUnitsByItemCodeAsync(item.Code);
        Details.Add(new ProductDestructionDetailEditModel
        {
            LineNumber = Details.Count + 1,
            ItemCode = item.Code,
            ItemName = item.Name,
            Qty = 1,
            UnitCode = item.UnitCost,
            WhCode = WhCode,
            ShelfCode = ShelfCode,
            AvailableUnits = units.ToList()
        });
    }

    [RelayCommand]
    private void RemoveItem(ProductDestructionDetailEditModel detail)
    {
        Details.Remove(detail);
        NormalizeLineNumbers();
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (PageNumber <= 1)
            return;

        PageNumber--;
        await LoadPageAsync();
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (PageNumber >= TotalPages)
            return;

        PageNumber++;
        await LoadPageAsync();
    }

    public async Task<IReadOnlyList<ErpItemDto>> SearchItemsAsync(string keyword) =>
        string.IsNullOrWhiteSpace(keyword)
            ? await _erpItemRepository.GetAllItemsAsync()
            : await _erpItemRepository.SearchItemsAsync(keyword);

    private void LoadEditor(ProductDestructionDto document)
    {
        IsExistingDocument = true;
        IsEditing = true;
        DocDate = document.DocDate.ToDateTime(TimeOnly.MinValue);
        DocNo = document.DocNo;
        Remark = document.Remark;
        WhCode = document.WhCode;
        ShelfCode = document.ShelfCode;

        Pictures.Clear();
        foreach (var picture in document.Pictures.OrderBy(p => p.LineNumber))
        {
            Pictures.Add(new ProductDestructionPictureEditModel
            {
                LineNumber = picture.LineNumber,
                ImageGuid = picture.ImageGuid,
                ImageFile = picture.ImageFile
            });
        }

        Details.Clear();
        foreach (var detail in document.Details.OrderBy(d => d.LineNumber))
        {
            Details.Add(new ProductDestructionDetailEditModel
            {
                LineNumber = detail.LineNumber,
                ItemCode = detail.ItemCode,
                ItemName = detail.ItemName,
                Qty = detail.Qty,
                UnitCode = detail.UnitCode,
                WhCode = detail.WhCode,
                ShelfCode = detail.ShelfCode
            });
        }
    }

    private void AddBlankDetail()
    {
        Details.Add(new ProductDestructionDetailEditModel
        {
            LineNumber = Details.Count + 1,
            Qty = 1,
            WhCode = WhCode,
            ShelfCode = ShelfCode
        });
    }

    private void NormalizeLineNumbers()
    {
        for (var index = 0; index < Pictures.Count; index++)
            Pictures[index].LineNumber = (short)(index + 1);

        for (var index = 0; index < Details.Count; index++)
            Details[index].LineNumber = index + 1;
    }

    private static string GenerateDocNo() => $"PD-{DateTime.Now:yyyyMMdd-HHmmss}";
}

public partial class ProductDestructionPictureEditModel : ObservableObject
{
    [ObservableProperty] private short _lineNumber;
    [ObservableProperty] private string _imageGuid = string.Empty;
    [ObservableProperty] private byte[] _imageFile = [];
    [ObservableProperty] private Bitmap? _previewImage;

    public string DisplayName => ImageGuid;
    public string SizeText => $"{ImageFile.Length / 1024.0:N1} KB";

    partial void OnImageGuidChanged(string value) => OnPropertyChanged(nameof(DisplayName));
    partial void OnImageFileChanged(byte[] value)
    {
        OnPropertyChanged(nameof(SizeText));
        PreviewImage = null;

        if (value.Length == 0)
            return;

        try
        {
            PreviewImage = new Bitmap(new MemoryStream(value));
        }
        catch (InvalidOperationException)
        {
            PreviewImage = null;
        }
    }
}

public partial class ProductDestructionDetailEditModel : ObservableObject
{
    [ObservableProperty] private int _lineNumber;
    [ObservableProperty] private string _itemCode = string.Empty;
    [ObservableProperty] private string _itemName = string.Empty;
    [ObservableProperty] private decimal _qty = 1m;
    [ObservableProperty] private string _unitCode = string.Empty;
    [ObservableProperty] private string _whCode = string.Empty;
    [ObservableProperty] private string _shelfCode = string.Empty;

    public List<ErpUnitDto> AvailableUnits { get; set; } = [];
}
