# Metrics Lab: chọn key để prefetch vào cache

Lab C# độc lập dùng điểm dự đoán cố định, không huấn luyện mô hình hoặc thay đổi cache đang chạy.
Mục tiêu là hiểu precision, recall, F1 và chọn metric theo chi phí nghiệp vụ.

## Chạy

Cần .NET 9 SDK. Từ thư mục gốc repo:

```bash
dotnet run --project samples/Caching.MetricsLab
dotnet test tests/Caching.MetricsLab.Tests/Caching.MetricsLab.Tests.csproj
```

Console in bảng threshold, confusion matrix, số key prefetch, các metric và điểm precision–recall.
File `precision-recall.svg` được ghi trong thư mục chạy hiện tại: mở bằng trình duyệt để xem biểu đồ validation.

## Nhãn và công thức

Mỗi dòng CSV là một key ở một thời điểm quan sát. `actual=1` nghĩa là key thực sự được truy cập
trong 5 phút tiếp theo; `score` nằm trong [0,1], chỉ là điểm minh họa, chưa phải xác suất được hiệu chỉnh.
Quyết định prefetch khi `score >= threshold` (bằng ngưỡng vẫn là dương tính).
Trong dữ liệu thật, feature chỉ dùng lịch sử trước thời điểm dự đoán; nhãn lấy từ truy cập tương lai,
không lấy từ quyết định prefetch hay cache hit.

| Kết quả | Dự đoán | Thực tế |
|---|---|---|
| TP | Prefetch | Có truy cập |
| FP | Prefetch | Không truy cập |
| FN | Không prefetch | Có truy cập |
| TN | Không prefetch | Không truy cập |

- Precision = TP / (TP + FP): tỷ lệ key prefetch thực sự được dùng.
- Recall = TP / (TP + FN): tỷ lệ key được truy cập mà ta đã chọn prefetch.
- F1 = 2TP / (2TP + FP + FN): trung bình điều hòa của precision và recall.
- Accuracy = (TP + TN) / tổng số mẫu.

Mẫu số bằng 0 được quy ước trả về 0; xem confusion matrix để nhận ra metric không xác định.
Ví dụ TP=2, FP=1, FN=1, TN=1: precision=recall=F1=2/3, accuracy=3/5.
Không suy ra F1 từ trung bình cộng precision và recall.

## Chọn metric

| Mục tiêu | Quy tắc chọn trên validation |
|---|---|
| Cân bằng | F1 cao nhất |
| Tài nguyên hạn chế | Recall cao nhất với precision >= 0.8 và tối đa 5 key prefetch |
| Cache miss đắt | Precision cao nhất với recall >= 0.8 |

Các ràng buộc là ví dụ, cần thay theo nghiệp vụ. Khi hòa, chọn ít prefetch hơn, sau đó ngưỡng cao hơn.
Không có ngưỡng hợp lệ thì trả `null`, không tự nới ràng buộc.
Ngân sách đếm key chỉ phù hợp lab: production cần xét kích thước byte, chi phí tải và thời gian lưu.
F1 không tính TN, không thể thay cho đo latency, hit rate, byte và backend load.

## Validation và test

`validation.csv` dùng để chọn ngưỡng; `test.csv` là tập giữ riêng để đánh giá ngưỡng đã chốt.
Chương trình chốt ngưỡng cho cả ba chính sách trước khi đọc nhãn test và không chọn lại dựa trên test.
Đây là dữ liệu tổng hợp, score có sẵn, nên không có bước train.
Nếu bổ sung mô hình, tách train/validation/test theo thời gian, tránh cửa sổ nhãn chồng lấn tại ranh giới;
fit mô hình/tiền xử lý trên train, chọn threshold trên validation và đánh giá test cuối cùng.
Biểu đồ là các điểm trên lưới 0.0–1.0 bước 0.1, không phải đường PR đầy đủ tại mọi score duy nhất.

`imbalanced.csv` có 100 key, chỉ 5 key được truy cập. Baseline luôn không prefetch đạt
accuracy 95% nhưng recall=F1=0. Điều đó không đạt mục tiêu tìm key cần prefetch.

## Bài tập

1. Tính tay bảng validation tại ngưỡng 0.3, 0.5, 0.7 rồi đối chiếu console.
2. Tăng precision tối thiểu lên 0.95; so sánh số key được chọn và recall.
3. Giảm ngân sách còn 2 key; kiểm tra chính sách trả ngưỡng nào hoặc không có nghiệm.
4. Giải thích tại sao threshold tốt nhất trên validation có thể giảm chất lượng trên test.
5. Thảo luận vì sao classifier có F1 cao chưa chắc làm cache nhanh hơn.
