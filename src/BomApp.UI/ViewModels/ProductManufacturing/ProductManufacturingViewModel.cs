using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BomApp.Application.Interfaces;
using BomApp.Application.Interfaces.Repositories;
using BomApp.Shared.Contracts;
using BomApp.UI.Services;

namespace BomApp.UI.ViewModels.ProductManufacturing;

public partial class ProductManufacturingViewModel : ViewModelBase
{
    private readonly IProductManufacturingService service;
    private readonly IErpItemRepository erpItemRepository;
    private readonly IDialogService dialogService;
    private bool hasLoadedInitialDocuments;

    public Func<Task<ErpItemDto?>>? ShowProductSearchDialog { get; set; }

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string docNoSearch = string.Empty;
    [ObservableProperty] private string itemCodeSearch = string.Empty;
    [ObservableProperty] private DateTimeOffset? dateFrom;
    [ObservableProperty] private DateTimeOffset? dateTo;
    [ObservableProperty] private ProductManufacturingDto? selectedDocument;
    [ObservableProperty] private int pageNumber = 1;
    [ObservableProperty] private int pageSize = 20;
    [ObservableProperty] private int totalCount;
    [ObservableProperty] private int totalPages = 1;
    [ObservableProperty] private bool isEditing;
    [ObservableProperty] private bool isExistingDocument;
    [ObservableProperty] private bool hasCalculatedMaterials;
    [ObservableProperty] private DateTimeOffset docDate = DateTimeOffset.Now;
    [ObservableProperty] private string docNo = string.Empty;
    [ObservableProperty] private string remark = string.Empty;
    [ObservableProperty] private string whCode = string.Empty;
    [ObservableProperty] private string shelfCode = string.Empty;

    public ObservableCollection<ProductManufacturingDto> Documents { get; } = new();
    public ObservableCollection<ProductManufacturingFinishGoodEditModel> FinishGoods { get; } = new();
    public ObservableCollection<ProductManufacturingMaterialEditModel> Materials { get; } = new();
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
    public string EditorTitle => IsExistingDocument ? "แก้ไขเอกสารผลิตสินค้า" : "เพิ่มเอกสารผลิตสินค้า";

    public ProductManufacturingViewModel(
        IProductManufacturingService service,
        IErpItemRepository erpItemRepository,
        IDialogService dialogService)
    {
        this.service = service;
        this.erpItemRepository = erpItemRepository;
        this.dialogService = dialogService;
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
        if (hasLoadedInitialDocuments)
            return;

        hasLoadedInitialDocuments = true;
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
            var dateFromValue = DateFrom.HasValue ? DateOnly.FromDateTime(DateFrom.Value.DateTime) : (DateOnly?)null;
            var dateToValue = DateTo.HasValue ? DateOnly.FromDateTime(DateTo.Value.DateTime) : (DateOnly?)null;

            var result = await service.GetDocumentsPageAsync(new ProductManufacturingListQuery(
                dateFromValue,
                dateToValue,
                string.IsNullOrWhiteSpace(DocNoSearch) ? null : DocNoSearch.Trim(),
                string.IsNullOrWhiteSpace(ItemCodeSearch) ? null : ItemCodeSearch.Trim(),
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
                ErrorMessage = result.Error ?? "ไม่สามารถโหลดรายการผลิตสินค้าได้";
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SelectDocumentAsync(ProductManufacturingDto document)
    {
        var result = await service.GetDocumentByDocNoAsync(document.DocNo);
        if (!result.IsSuccess || result.Value is null)
        {
            ErrorMessage = result.Error ?? "ไม่สามารถโหลดรายละเอียดเอกสารผลิตสินค้าได้";
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
        HasCalculatedMaterials = false;
        DocDate = DateTimeOffset.Now;
        DocNo = GenerateDocNo();
        Remark = string.Empty;
        WhCode = string.Empty;
        ShelfCode = string.Empty;
        FinishGoods.Clear();
        Materials.Clear();
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    private async Task AddFinishGoodAsync()
    {
        if (ShowProductSearchDialog is null)
            return;

        var item = await ShowProductSearchDialog();
        if (item is null)
            return;

        var units = await erpItemRepository.GetUnitsByItemCodeAsync(item.Code);
        FinishGoods.Add(new ProductManufacturingFinishGoodEditModel
        {
            LineNumber = FinishGoods.Count + 1,
            ItemCode = item.Code,
            ItemName = item.Name,
            Qty = 1,
            UnitCode = string.IsNullOrWhiteSpace(item.UnitCost) ? units.FirstOrDefault()?.Code ?? string.Empty : item.UnitCost,
            WhCode = WhCode,
            ShelfCode = ShelfCode,
            AvailableUnits = units.ToList()
        });
        HasCalculatedMaterials = false;
    }

    [RelayCommand]
    private void RemoveFinishGood(ProductManufacturingFinishGoodEditModel item)
    {
        FinishGoods.Remove(item);
        NormalizeLineNumbers();
        HasCalculatedMaterials = false;
    }

    [RelayCommand]
    private async Task CalculateAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            NormalizeLineNumbers();
            var result = await service.CalculateAsync(BuildCalculateRequest(dryRun: true));
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "ไม่สามารถคำนวณวัตถุดิบได้";
                return;
            }

            LoadCalculatedDocument(result.Value);
            HasCalculatedMaterials = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            NormalizeLineNumbers();
            var calculation = await service.CalculateAsync(BuildCalculateRequest(dryRun: true));
            if (!calculation.IsSuccess || calculation.Value is null)
            {
                ErrorMessage = calculation.Error ?? "ไม่สามารถคำนวณวัตถุดิบได้";
                return;
            }

            LoadCalculatedDocument(calculation.Value);
            var date = DateOnly.FromDateTime(DocDate.DateTime);
            var finishGoods = FinishGoods
                .Where(d => !string.IsNullOrWhiteSpace(d.ItemCode))
                .Select(d => new CreateProductManufacturingFinishGoodCommand(
                    d.ItemCode,
                    d.Qty,
                    d.UnitCode,
                    string.IsNullOrWhiteSpace(d.WhCode) ? WhCode : d.WhCode,
                    string.IsNullOrWhiteSpace(d.ShelfCode) ? ShelfCode : d.ShelfCode,
                    d.LineNumber))
                .ToList();
            var materials = Materials
                .Where(d => !string.IsNullOrWhiteSpace(d.ItemCode))
                .Select(d => new CreateProductManufacturingMaterialCommand(
                    d.ItemCode,
                    d.ItemName,
                    d.Qty,
                    d.UnitCode,
                    d.WhCode,
                    d.ShelfCode,
                    d.LineNumber))
                .ToList();

            var result = IsExistingDocument
                ? await service.UpdateAsync(DocNo, new UpdateProductManufacturingCommand(date, WhCode, ShelfCode, Remark, finishGoods, materials))
                : await service.CreateAsync(new CreateProductManufacturingCommand(DocNo, date, WhCode, ShelfCode, Remark, finishGoods, materials));

            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "ไม่สามารถบันทึกเอกสารผลิตสินค้าได้";
                return;
            }

            IsEditing = false;
            HasCalculatedMaterials = false;
            await LoadPageAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteDocumentAsync(ProductManufacturingDto document)
    {
        var confirmed = await dialogService.ConfirmAsync(
            "Confirm delete",
            $"Delete product manufacturing document '{document.DocNo}'?");
        if (!confirmed)
            return;

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var result = await service.DeleteAsync(document.DocNo);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "Unable to delete product manufacturing document.";
                return;
            }

            await LoadPageAsync();
        }
        finally
        {
            IsLoading = false;
        }
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
            ? await erpItemRepository.GetAllItemsAsync()
            : await erpItemRepository.SearchItemsAsync(keyword);

    private CalculateProductManufacturingRequest BuildCalculateRequest(bool dryRun) =>
        new(
            DateOnly.FromDateTime(DocDate.DateTime),
            DocNo,
            WhCode,
            ShelfCode,
            Remark,
            FinishGoods
                .Where(d => !string.IsNullOrWhiteSpace(d.ItemCode))
                .Select(d => new CreateProductManufacturingFinishGoodCommand(
                    d.ItemCode,
                    d.Qty,
                    d.UnitCode,
                    string.IsNullOrWhiteSpace(d.WhCode) ? WhCode : d.WhCode,
                    string.IsNullOrWhiteSpace(d.ShelfCode) ? ShelfCode : d.ShelfCode,
                    d.LineNumber))
                .ToList(),
            dryRun);

    private void LoadEditor(ProductManufacturingDto document)
    {
        IsExistingDocument = true;
        IsEditing = true;
        HasCalculatedMaterials = true;
        DocDate = document.DocDate.ToDateTime(TimeOnly.MinValue);
        DocNo = document.DocNo;
        Remark = document.Remark;
        WhCode = document.WhCode;
        ShelfCode = document.ShelfCode;
        LoadCalculatedDocument(document);
    }

    private void LoadCalculatedDocument(ProductManufacturingDto document)
    {
        FinishGoods.Clear();
        foreach (var finishGood in document.FinishGoods.OrderBy(d => d.LineNumber))
        {
            FinishGoods.Add(new ProductManufacturingFinishGoodEditModel
            {
                LineNumber = finishGood.LineNumber,
                ItemCode = finishGood.ItemCode,
                ItemName = finishGood.ItemName,
                Qty = finishGood.Qty,
                UnitCode = finishGood.UnitCode,
                WhCode = finishGood.WhCode,
                ShelfCode = finishGood.ShelfCode
            });
        }

        Materials.Clear();
        foreach (var material in document.Materials.OrderBy(d => d.LineNumber))
        {
            Materials.Add(new ProductManufacturingMaterialEditModel
            {
                LineNumber = material.LineNumber,
                ItemCode = material.ItemCode,
                ItemName = material.ItemName,
                Qty = material.Qty,
                UnitCode = material.UnitCode,
                WhCode = material.WhCode,
                ShelfCode = material.ShelfCode
            });
        }
    }

    private void NormalizeLineNumbers()
    {
        for (var index = 0; index < FinishGoods.Count; index++)
            FinishGoods[index].LineNumber = index + 1;

        for (var index = 0; index < Materials.Count; index++)
            Materials[index].LineNumber = index + 1;
    }

    private static string GenerateDocNo() => $"MP-{DateTime.Now:yyyyMMdd-HHmmss}";
}

public partial class ProductManufacturingFinishGoodEditModel : ObservableObject
{
    [ObservableProperty] private int lineNumber;
    [ObservableProperty] private string itemCode = string.Empty;
    [ObservableProperty] private string itemName = string.Empty;
    [ObservableProperty] private decimal qty = 1m;
    [ObservableProperty] private string unitCode = string.Empty;
    [ObservableProperty] private string whCode = string.Empty;
    [ObservableProperty] private string shelfCode = string.Empty;

    public List<ErpUnitDto> AvailableUnits { get; set; } = [];
}

public partial class ProductManufacturingMaterialEditModel : ObservableObject
{
    [ObservableProperty] private int lineNumber;
    [ObservableProperty] private string itemCode = string.Empty;
    [ObservableProperty] private string itemName = string.Empty;
    [ObservableProperty] private decimal qty;
    [ObservableProperty] private string unitCode = string.Empty;
    [ObservableProperty] private string whCode = string.Empty;
    [ObservableProperty] private string shelfCode = string.Empty;
}
