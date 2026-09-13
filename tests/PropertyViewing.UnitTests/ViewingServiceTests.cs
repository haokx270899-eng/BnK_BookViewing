using Microsoft.Extensions.Logging.Abstractions;
using PropertyViewing.Application.Exceptions;
using PropertyViewing.Application.Interfaces;
using PropertyViewing.Application.Services;
using PropertyViewing.Domain.Entities;
using Xunit;

namespace PropertyViewing.UnitTests;

public sealed class ViewingServiceTests
{
    private const string DefaultTimeZone = "Europe/London"; // UTC+1 vào tháng 9 (BST)

    [Fact]
    public async Task BookAsync_ValidAlignedSlot_CreatesViewing()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        // 10:30 Local London (BST = UTC+1) -> 09:30 UTC
        var startUtc = AtUtc(9, 30);
        var result = await service.BookAsync(new(1, 1, startUtc), default);

        Assert.Equal(AtUtc(10, 0), result.EndTime); // EndTime = 10:00 UTC (11:00 Local)
        Assert.Single(repository.Viewings);
    }

    [Fact]
    public async Task BookAsync_CrossTimeZone_ValidatesLocalBusinessHoursCorrectly()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        // Khách ở VN (UTC+7) muốn xem nhà ở London (UTC+1) lúc 16:30 giờ VN (tức 10:30 giờ London)
        // 16:30 VN (UTC+7) = 09:30 UTC
        var requestUtc = AtUtc(9, 30);

        var result = await service.BookAsync(new(1, 1, requestUtc), default);

        Assert.NotNull(result);
        Assert.Equal(requestUtc, result.StartTime);
    }

    [Fact]
    public async Task BookAsync_AlreadyBookedSlot_ThrowsConflict()
    {
        var repository = new FakeRepository();
        var startUtc = AtUtc(9, 30); // 10:30 Local London
        await repository.CreateAsync(NewViewing(1, startUtc), default);

        await Assert.ThrowsAsync<BookingConflictException>(() =>
            CreateService(repository).BookAsync(new(1, 2, startUtc), default));
    }

    [Theory]
    [InlineData(7, 30)]  // 07:30 UTC = 08:30 Local London (Sớm hơn 09:00 Local) -> Reject
    [InlineData(19, 0)]  // 19:00 UTC = 20:00 Local London (Hết giờ làm việc 20:00 Local) -> Reject
    [InlineData(9, 15)]  // 09:15 UTC = 10:15 Local London (Lẻ 15 phút, không đúng mốc 30 phút) -> Reject
    public async Task BookAsync_InvalidHoursOrAlignment_IsRejected(int utcHour, int minute) =>
        await Assert.ThrowsAsync<ValidationException>(() =>
            CreateService(new FakeRepository()).BookAsync(new(1, 1, AtUtc(utcHour, minute)), default));

    [Fact]
    public async Task BookAsync_NonExistentProperty_IsNotFound()
    {
        var repository = new FakeRepository();
        repository.Properties.Clear();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateService(repository).BookAsync(new(99, 1, AtUtc(9, 30)), default));
    }

    [Fact]
    public async Task BookAsync_NonExistentUser_IsNotFound()
    {
        var repository = new FakeRepository { Users = [] };

        await Assert.ThrowsAsync<NotFoundException>(() =>
            CreateService(repository).BookAsync(new(1, 99, AtUtc(9, 30)), default));
    }

    [Fact]
    public async Task BookAsync_InvalidTimeZoneInProperty_ThrowsValidationException()
    {
        var repository = new FakeRepository();
        repository.Properties[1] = "Invalid/TimeZone_Name";

        await Assert.ThrowsAsync<ValidationException>(() =>
            CreateService(repository).BookAsync(new(1, 1, AtUtc(9, 30)), default));
    }

    [Fact]
    public async Task AvailableAsync_ExcludesBookedSlot()
    {
        var repository = new FakeRepository();

        // Slot đã book: 09:30 UTC - 10:00 UTC (Tương ứng 10:30 - 11:00 Local London)
        var bookedStartUtc = AtUtc(9, 30);
        await repository.CreateAsync(NewViewing(1, bookedStartUtc), default);

        var date = DateOnly.FromDateTime(AtUtc(0, 0));
        var slots = await CreateService(repository).GetAvailableAsync(1, date, date, default);

        // Đảm bảo không chứa slot 09:30 UTC đã book
        Assert.DoesNotContain(slots, x => x.StartTime == bookedStartUtc);

        // Slot 08:00 UTC (tương ứng 09:00 Local London) phải còn trống
        Assert.Contains(slots, x => x.StartTime == AtUtc(8, 0));
    }

    [Fact]
    public async Task AvailableAsync_MultipleDays_ReturnsSlotsForEachDay()
    {
        var service = CreateService(new FakeRepository());
        var date = DateOnly.FromDateTime(AtUtc(0, 0));

        var slots = await service.GetAvailableAsync(1, date, date.AddDays(1), default);

        // Mỗi ngày làm việc từ 09:00 - 20:00 Local có 22 slots (mỗi slot 30 phút)
        // 2 ngày = 44 slots UTC
        Assert.Equal(44, slots.Count);
    }

    private static ViewingService CreateService(FakeRepository repository) =>
        new(repository);

    private static DateTime AtUtc(int hour, int minute) =>
        new(2026, 9, 15, hour, minute, 0, DateTimeKind.Utc);

    private static Viewing NewViewing(int propertyId, DateTime startUtc) => new()
    {
        PropertyId = propertyId,
        UserId = 1,
        StartTime = startUtc,
        EndTime = startUtc.AddMinutes(30),
        CreatedAt = DateTime.UtcNow
    };
}

public sealed class FakeRepository : IViewingRepository
{
    public Dictionary<int, string> Properties { get; set; } = new()
    {
        { 1, "Europe/London" },
        { 2, "Europe/London" },
        { 3, "Europe/London" }
    };

    public HashSet<int> Users { get; set; } = [1, 2, 3];
    public List<Viewing> Viewings { get; } = [];

    public Task<string?> GetPropertyTimeZoneAsync(int propertyId, CancellationToken cancellationToken) =>
        Task.FromResult(Properties.TryGetValue(propertyId, out var tz) ? tz : null);

    public Task<bool> PropertyExistsAsync(int id, CancellationToken token) =>
        Task.FromResult(Properties.ContainsKey(id));

    public Task<bool> UserExistsAsync(int id, CancellationToken token) =>
        Task.FromResult(Users.Contains(id));

    public Task<bool> HasConflictAsync(int propertyId, DateTime start, DateTime end, CancellationToken token) =>
        Task.FromResult(Viewings.Any(x => x.PropertyId == propertyId && x.StartTime < end && x.EndTime > start));

    public Task<IReadOnlyList<Viewing>> GetByPropertyAndDateRangeAsync(
        int propertyId, DateTime from, DateTime to, CancellationToken token) =>
        Task.FromResult<IReadOnlyList<Viewing>>(
            Viewings.Where(x => x.PropertyId == propertyId && x.StartTime < to && x.EndTime > from).ToList());

    public Task<Viewing> CreateAsync(Viewing viewing, CancellationToken token)
    {
        viewing.Id = Viewings.Count + 1;
        Viewings.Add(viewing);
        return Task.FromResult(viewing);
    }
}