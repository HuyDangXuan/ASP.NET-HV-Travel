# AI Integration Audit

## Summary

Tai lieu nay chot hien trang AI cua repo `HV Travel` theo 4 trang thai:

- `Da co`
- `Co nen tang nhung chua dat`
- `Chua co`
- `Ngoai scope`

Muc tieu cua audit:

- xac nhan tinh nang nao da ton tai bang code dang wired that
- tach ro AI that su voi heuristic/noi suy noi bo
- chon 1 huong uu tien tiep theo phu hop voi domain du lich

## Audit Matrix

| Hang muc | Trang thai | Evidence trong repo | Nhan xet |
|---|---|---|---|
| Chatbot | Da co | `HV-Travel.Web/Program.cs`, `HV-Travel.Web/Controllers/TourAiChatController.cs`, `HV-Travel.Web/Hubs/TourAiChatHub.cs`, `HV-Travel.Web/Services/TourAiChatService.cs`, `HV-Travel.Web/Services/GroqChatClient.cs` | Day la AI integration hop le vi da co controller, hub, background worker, prompt context va LLM client goi Groq theo OpenAI-compatible API. |
| Du doan va phan tich giao thong | Co nen tang nhung chua dat | `HV-Travel.Application/Services/RouteTravelEstimator.cs`, `HV-Travel.Application/Services/RouteOptimizationService.cs`, `HV-Travel.Application/Services/TripPlannerService.cs`, `HV-Travel.Web.Tests/RouteIntelligencePhase3Tests.cs` | Hien tai moi dat muc `traffic-aware heuristic`: uoc luong theo distance, day-part, congestion level va route style. Chua co mo hinh du doan, traffic API realtime hay du lieu hoc may. |
| Tim kiem toi uu keyword bang Elasticsearch/Meilisearch | Chua co | `HV-Travel.Infrastructure/Repositories/TourRepository.cs`, `HV-Travel.Application/Services/TourSearchService.cs` | Search hien tai van dua tren Mongo regex va ranking tai tang application. Chua co package, index backend, sync pipeline hay fallback search-engine aware. |
| OCR | Chua co | Khong tim thay package, service, controller, upload flow hay pipeline doc anh thanh text | Chua co dau vet Tesseract, OCR SDK hay image-to-text workflow. |
| Tro ly giong noi | Chua co | Khong tim thay speech recognition, microphone, audio capture, TTS/STT hay voice command flow | Chua co frontend microphone UX va khong co backend xu ly audio. |
| Chan doan y te | Ngoai scope | Khong co domain model, use case hay UI lien quan y te | Khong phu hop bai toan website tour/du lich, khong nen dua vao roadmap uu tien de bao ve do an. |
| Thuc te ao | Chua co | Khong tim thay WebXR, A-Frame, Three.js, 360 viewer hay asset 3D | Co the demo dep, nhung hien tai repo khong co nen tang cho huong nay. |

## Why Chatbot Counts As AI

Chatbot hien co khong chi la support chat realtime thong thuong:

- `Program.cs` dang wire `IGroqChatClient`, `ITourAiChatService`, `TourAiReplyWorker` va `TourAiChatHub`.
- `TourAiChatService` xay dung ngu canh hoi dap theo tour, luu lich su hoi thoai va enqueue job tra loi.
- `GroqChatClient` goi model `llama-3.3-70b-versatile` qua endpoint OpenAI-compatible.
- `TourAiRouteAdvisorContextBuilder` bo sung snapshot theo tour, departures, route insight va related tours de lam context cho AI.

Ket luan: chatbot hien tai duoc tinh la AI integration end-to-end.

## Boundary For Traffic Claim

Repo co route intelligence va trip planner, nhung khi demo hoac viet bao cao nen dung cau mo ta sau:

- Nen ghi: `traffic-aware heuristic`, `route intelligence`, `trip planner`.
- Khong nen ghi: `traffic prediction AI` hoac `du doan giao thong bang mo hinh hoc may`.

Ly do:

- chua co training data
- chua co external traffic provider
- chua co forecasting model
- chua co so sanh du doan voi ground truth

## Recommended Next Priority

Huong uu tien tiep theo: `Meilisearch cho tim kiem tour`.

### Tai sao uu tien huong nay

- Phu hop truc tiep voi domain du lich va hanh vi tim tour theo keyword.
- De demo: co the cho thay ket qua tim kiem nhanh hon, dung hon, chiu typo tot hon.
- Tan dung duoc flow search/filter hien co ma khong phai viet lai UI tu dau.
- De bao ve do an hon OCR, voice assistant hoac VR trong boi canh website tour.

### Pham vi de xuat cho phase tiep theo

- Giu `PublicToursController` va `ITourSearchService` lam entrypoint hien tai.
- Them abstraction search backend o tang application, vi du `ITourSearchBackend`.
- Tao search document rieng cho tour:
  - name
  - short description
  - destination
  - highlights
  - starting price
  - rating
  - public status
  - routing summary
- Dong bo index khi tour duoc tao, sua, import hoac doi trang thai public.
- Giu fallback ve Mongo search neu search engine tam thoi khong san sang.

## Do Not Prioritize Yet

- `Medical diagnosis`: ngoai scope.
- `Traffic prediction`: ton cong lon hon va chua co du lieu/ha tang de claim thuyet phuc.
- `OCR`: kha nang demo duoc, nhung do gan voi core travel thap hon search nang cao.
- `Voice assistant`: dep khi demo, nhung gia tri chuc nang cho public tour listing thap hon search.
- `VR`: rat ton cong ve asset va frontend, khong phu hop uu tien ngan han.

## Verification Sources Used In This Audit

- `HV-Travel.Web/Program.cs`
- `HV-Travel.Web/Services/GroqChatClient.cs`
- `HV-Travel.Web/Services/TourAiChatService.cs`
- `HV-Travel.Web/Services/TourAiRouteAdvisorContextBuilder.cs`
- `HV-Travel.Application/Services/RouteTravelEstimator.cs`
- `HV-Travel.Application/Services/RouteOptimizationService.cs`
- `HV-Travel.Application/Services/TripPlannerService.cs`
- `HV-Travel.Application/Services/TourSearchService.cs`
- `HV-Travel.Infrastructure/Repositories/TourRepository.cs`
- `HV-Travel.Web.Tests/RouteIntelligencePhase3Tests.cs`

## Final Recommendation

Neu can them 1 hang muc AI de tang diem va van giu dung domain travel, phase tiep theo nen la:

`Meilisearch-backed tour search with Mongo fallback`

Day la huong co gia tri thuc te, de demo, de giai thich, va phu hop nhat voi hien trang codebase.
