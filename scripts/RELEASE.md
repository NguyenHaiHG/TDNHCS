# Quy trình phát hành (dành cho developer)

Repo GitHub: **NguyenHaiHG/TDNHCS** — cần để **Public** để máy client tự cập nhật (không cần token).

## Phát hành bản mới

```powershell
cd D:\QLVBNHCS
.\scripts\PublishRelease.ps1 -Version 1.0.2 -Notes "Mô tả thay đổi"
```

Hoặc chỉ build installer:

```powershell
.\scripts\BuildInstaller.ps1
```

## Tạo Release trên GitHub

1. Vào https://github.com/NguyenHaiHG/TDNHCS/releases/new
2. Tag: `v1.0.2` (khớp version trong csproj)
3. Upload: `dist\TDNHCS_Setup.exe`
4. Publish release

## Quy tắc version

| Nơi | Ví dụ |
|-----|-------|
| `TDNHCS.csproj` → `<Version>` | `1.0.2` |
| GitHub tag | `v1.0.2` |

Người dùng cuối **không thấy** thông tin GitHub trong phần mềm — cấu hình nằm hardcode trong `UpdateConfig.cs`.
