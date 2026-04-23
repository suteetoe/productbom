# Design Tokens — BOM Production Calculator
> Team B บันทึกค่า design tokens ที่นี่ เพื่อให้ทุก screen มี UI สม่ำเสมอ
> ทุก screen ต้องใช้ค่าจากไฟล์นี้เท่านั้น — ห้าม hardcode ค่าใน AXAML โดยตรง

---

## Color Palette

```xml
<!-- Avalonia Application.Resources -->
<ResourceDictionary>
  <!-- Primary -->
  <Color x:Key="AccentBlue">#3B82F6</Color>
  <Color x:Key="AccentBlueDark">#1D4ED8</Color>

  <!-- Neutral -->
  <Color x:Key="Surface">#FFFFFF</Color>
  <Color x:Key="SurfaceSecondary">#F8FAFC</Color>
  <Color x:Key="Border">#E2E8F0</Color>
  <Color x:Key="TextPrimary">#0F172A</Color>
  <Color x:Key="TextSecondary">#64748B</Color>
  <Color x:Key="TextMuted">#94A3B8</Color>

  <!-- Status -->
  <Color x:Key="Success">#22C55E</Color>
  <Color x:Key="Warning">#F59E0B</Color>
  <Color x:Key="Danger">#EF4444</Color>
  <Color x:Key="Info">#3B82F6</Color>
</ResourceDictionary>
```

---

## Typography

```xml
<!-- Font sizes -->
<x:Double x:Key="FontSizeXS">11</x:Double>
<x:Double x:Key="FontSizeSM">12</x:Double>
<x:Double x:Key="FontSizeBase">14</x:Double>
<x:Double x:Key="FontSizeLG">16</x:Double>
<x:Double x:Key="FontSizeXL">18</x:Double>
<x:Double x:Key="FontSizeH2">20</x:Double>
<x:Double x:Key="FontSizeH1">24</x:Double>
```

---

## Spacing

```xml
<!-- Spacing scale -->
<Thickness x:Key="SpaceXS">4</Thickness>
<Thickness x:Key="SpaceSM">8</Thickness>
<Thickness x:Key="SpaceMD">12</Thickness>
<Thickness x:Key="SpaceLG">16</Thickness>
<Thickness x:Key="SpaceXL">24</Thickness>
<Thickness x:Key="Space2XL">32</Thickness>

<!-- Card / Panel padding -->
<Thickness x:Key="PanelPadding">16,12,16,12</Thickness>
<Thickness x:Key="CardPadding">20,16,20,16</Thickness>
```

---

## Layout Constants

```xml
<!-- Sidebar -->
<x:Double x:Key="SidebarWidth">220</x:Double>

<!-- Toolbar -->
<x:Double x:Key="ToolbarHeight">60</x:Double>

<!-- DataGrid row -->
<x:Double x:Key="DataGridRowHeight">36</x:Double>

<!-- Border radius -->
<CornerRadius x:Key="RadiusSM">4</CornerRadius>
<CornerRadius x:Key="RadiusMD">6</CornerRadius>
<CornerRadius x:Key="RadiusLG">8</CornerRadius>
```

---

## Screen Implementation Log

> Team B บันทึกทุกครั้งที่ implement screen ใหม่

| Screen | วันที่ | หมายเหตุ |
|---|---|---|
| (ยังไม่มี) | — | — |

---

## การอัปเดต Design Tokens

เมื่อ Team B ต้องการอัปเดต tokens:
1. เปรียบเทียบกับค่าที่มีอยู่ในไฟล์นี้
2. ถ้าต่างจากเดิมมากกว่า 20% → แจ้ง CTO ก่อน update
3. บันทึกใน Screen Implementation Log ด้านบน
