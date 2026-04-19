# Phase 2 Progress

## Summary

Phase 2 da dua repo len ngang chat context cho nhom tinh nang `routing` va `departures`.
Tour hien co the import, luu, sua va render du lieu route-ready ma khong lam vo schema commerce moi.

## Delivered

- Them domain model `Tour.Routing` voi `TourRouting`, `TourRouteStop` va `GeoPoint`.
- Khoa mat du lieu schema moi trong admin `Create`, `Edit` va `Import`.
- Ho tro import `Tours.json` theo Mongo extended JSON va JSON thuong.
- Chuan hoa public tour links theo `slug` truoc, `id` sau; fix crash khi truy cap bang slug.
- Them helper public identifier dung chung cho canonical URL va cac link detail.
- Mo admin read-only cho `slug`, `seo`, `departures`, `routing`.
- Mo admin read/write cho `departures` va `routing` trong form tao/sua tour.
- Them public route preview doc truc tiep tu `Tour.Routing`.
- Chuan hoa sanitize route text truoc khi render public.
- Bo sung test project `HV-Travel.Web.Tests` cho slug routing, import, admin schema-safe va public route preview.

## Verification

Lenh xac nhan phase 2:

```powershell
dotnet test "HV-Travel.Web.Tests/HV-Travel.Web.Tests.csproj"
```

Ket qua khi chot phase 2: `11/11` test pass.

## Deferred

- Chua co map API, marker hay polyline.
- Chua co ETA heuristic, traffic heuristic hay route metrics dan xuat.
- Chua co TSP reorder, recommendation engine hay multi-criteria optimizer.
- Chua dua route insight vao AI snapshot.

## Known Repo State

- Repo van con mot so warning nullable cu khong phai regression cua phase 2.
- Co canh bao `NU1902` lien quan `MailKit 4.15.0`; phase 2 khong mo rong pham vi xu ly dependency nay.

## Next Recommended Phase

Phase tiep theo nen la `Phase 3: Route Intelligence Core`:

- Tinh quang duong, travel time heuristic va journey time tu `Tour.Routing`.
- Hien thi metric route tren public detail va admin details.
- Dua tom tat lo trinh vao AI tour snapshot ma khong lo raw coordinates.
