using BomApp.Shared.Contracts;

namespace BomApp.Tests.Fakes;

public static class SeedData
{
    // === ERP Items ===
    // Items WITH BOM assignment (happy path)
    public static readonly ErpItemDto ItemWithBom1 = new("PROD-001", "สินค้า A (มี BOM)", "PCS");
    public static readonly ErpItemDto ItemWithBom2 = new("PROD-002", "สินค้า B (มี BOM)", "KG");

    // Item WITHOUT BOM (triggers warning path)
    public static readonly ErpItemDto ItemWithoutBom = new("PROD-999", "สินค้าไม่มี BOM", "PCS");

    // === Units for PROD-001 (multi-unit conversion test) ===
    // 1 PCS = 1 PCS (base unit)
    public static readonly ErpUnitDto Unit_PROD001_PCS = new("PCS",  "ชิ้น",  "PROD-001",  1m,  1m,   1, 1);
    // 1 BOX = 12 PCS
    public static readonly ErpUnitDto Unit_PROD001_BOX = new("BOX",  "กล่อง", "PROD-001", 12m,  1m,  12, 2);
    // 1 CTN = 144 PCS
    public static readonly ErpUnitDto Unit_PROD001_CTN = new("CTN",  "ลัง",   "PROD-001", 144m, 1m, 144, 3);

    // === Units for PROD-002 ===
    public static readonly ErpUnitDto Unit_PROD002_KG = new("KG", "กิโลกรัม", "PROD-002", 1m, 1m, 1, 1);

    // === Sales Transactions ===
    // Day 1 (2024-01-15) — two documents
    // Doc SO-2024-0001: 10 PCS of PROD-001
    public static readonly ErpSalesTransactionDto Sales_Day1_Doc1_PROD001 = new(
        new DateOnly(2024, 1, 15), "SO-2024-0001", "PROD-001", 10m, "PCS", 1m, 1m);

    // Doc SO-2024-0002: 5 BOX of PROD-001 (= 60 PCS in base unit)
    public static readonly ErpSalesTransactionDto Sales_Day1_Doc2_PROD001 = new(
        new DateOnly(2024, 1, 15), "SO-2024-0002", "PROD-001", 5m, "BOX", 12m, 1m);

    // Doc SO-2024-0001: 2 PCS of PROD-999 (no BOM — should warn and skip)
    public static readonly ErpSalesTransactionDto Sales_Day1_Doc1_PROD999 = new(
        new DateOnly(2024, 1, 15), "SO-2024-0001", "PROD-999", 2m, "PCS", 1m, 1m);

    // Day 2 (2024-01-16) — one document
    // Doc SO-2024-0003: 20 KG of PROD-002
    public static readonly ErpSalesTransactionDto Sales_Day2_Doc1_PROD002 = new(
        new DateOnly(2024, 1, 16), "SO-2024-0003", "PROD-002", 20m, "KG", 1m, 1m);
}
