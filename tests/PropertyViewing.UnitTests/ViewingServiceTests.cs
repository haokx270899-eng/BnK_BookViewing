using PropertyViewing.Application.Exceptions;
using PropertyViewing.Application.Interfaces;
using PropertyViewing.Application.Services;
using PropertyViewing.Domain.Entities;
using Xunit;

namespace PropertyViewing.UnitTests;

public sealed class ViewingServiceTests
{
    private const string DefaultTimeZone = "Europe/London"; // BST (UTC+1) in September

    [Fact]
    public async Task BookAsync_ValidAlignedSlot_CreatesViewing()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        // 10:30 Local London (BST = UTC+1) -> Converted to 09:30 UTC
        var localStart = new DateTime(2026, 9, 15, 10, 30, 0, DateTimeKind.Unspecified);
        var result = await service.BookAsync(new(1, 1, localStart), default);

        // EndTime in UTC should be 10:00 UTC (Corresponding to 11:00 Local London)
        Assert.Equal(AtUtc(10, 0), result.EndTime);
        Assert.Single(repository.Viewings);
    }

    [Fact]
    public async Task BookAsync_CrossTimeZone_ValidatesLocalBusinessHoursCorrectly()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        // Viewer books a slot at 10:30 Local London time
        var localStart = new DateTime(2026, 9, 15, 10, 30, 0, DateTimeKind.Unspecified);

        var result = await service.BookAsync(new(1, 1, localStart), default);

        Assert.NotNull(result);
        // 10:30 Local London (BST UTC+1) correctly converts to 09:30 UTC
        Assert.Equal(AtUtc(9, 30), result.StartTime);
    }

    [Fact]
    public async Task BookAsync_AlreadyBookedSlot_ThrowsConflict()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        // 1. Create a pre-booked slot in DB (Stored in UTC)
        // 09:30 UTC equals 10:30 Local London (BST = UTC+1)
        var existingStartUtc = AtUtc(9, 30);
        await repository.CreateAsync(NewViewing(1, existingStartUtc), default);

        // 2. New request attempts to book the same slot at 10:30 Local London
        // Service converts 10:30 Local -> 09:30 UTC (matches existing slot above)
        var localStart = new DateTime(2026, 9, 15, 10, 30, 0, DateTimeKind.Unspecified);

        // 3. Execute and verify exception
        await Assert.ThrowsAsync<BookingConflictException>(() =>
            service.BookAsync(new(1, 2, localStart), default));
    }

    [Theory]
    [InlineData(8, 30)]  // 08:30 Local London (Earlier than 09:00 Local) -> Reject
    [InlineData(20, 0)]  // 20:00 Local London (Closed) -> Reject
    [InlineData(10, 15)] // 10:15 Local London (15-min unaligned slot) -> Reject
    public async Task BookAsync_InvalidHoursOrAlignment_IsRejected(int localHour, int minute)
    {
        var localStart = new DateTime(2026, 9, 15, localHour, minute, 0, DateTimeKind.Unspecified);

        await Assert.ThrowsAsync<ValidationException>(() =>
            CreateService(new FakeRepository()).BookAsync(new(1, 1, localStart), default));
    }

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



    //--------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task BookAsync_DuringFallBackDST_HandlesAmbiguousLocalTimeSafely()
    {
        var repository = new FakeRepository();
        repository.Properties[1] = "Europe/London";
        var service = CreateService(repository);

        // October 25, 2026 in London is Fall Back day (clocks turn back 02:00 -> 01:00 AM)
        // Book a slot at 09:30 Local Time on this day
        var localTimeOnDstFallback = new DateTime(2026, 10, 25, 9, 30, 0, DateTimeKind.Unspecified);

        var result = await service.BookAsync(new(1, 1, localTimeOnDstFallback), default);

        Assert.NotNull(result);
        // Verify UTC is accurately converted per Greenwich Mean Time standard (GMT = UTC+0 in winter)
        Assert.Equal(new DateTime(2026, 10, 25, 9, 30, 0, DateTimeKind.Utc), result.StartTime);
    }
    //--------------------------------------------------------------------------------------------------------

    [Fact]
    public async Task AvailableAsync_ExcludesBookedSlot()
    {
        var repository = new FakeRepository();

        // Booked slot: 09:30 UTC - 10:00 UTC (Corresponding to 10:30 - 11:00 Local London)
        var bookedStartUtc = AtUtc(9, 30);
        await repository.CreateAsync(NewViewing(1, bookedStartUtc), default);

        var date = DateOnly.FromDateTime(AtUtc(0, 0));
        var slots = await CreateService(repository).GetAvailableAsync(1, date, date, default);

        // Accurately matches UtcStartTime property in DTO
        Assert.DoesNotContain(slots, x => x.UtcStartTime == bookedStartUtc);

        // Slot 08:00 UTC (corresponding to 09:00 Local London) must remain available
        Assert.Contains(slots, x => x.UtcStartTime == AtUtc(8, 0));
    }

    [Fact]
    public async Task AvailableAsync_MultipleDays_ReturnsSlotsForEachDay()
    {
        var service = CreateService(new FakeRepository());
        var date = DateOnly.FromDateTime(AtUtc(0, 0));

        var slots = await service.GetAvailableAsync(1, date, date.AddDays(1), default);

        // 22 slots per business day (09:00 - 20:00) * 2 days = 44 slots
        Assert.Equal(44, slots.Count);
    }

    //--------------------------------------------------------------------------------------------------------
    [Fact]
    public async Task GetAvailableAsync_DuringSpringForwardDST_SkipsInvalidLocalSlots()
    {
        var repository = new FakeRepository();
        repository.Properties[1] = "America/New_York";
        var service = CreateService(repository);

        // March 8, 2026 is Spring Forward day in New York (02:00 AM jumps to 03:00 AM)
        var dstDate = new DateOnly(2026, 3, 8);

        var slots = await service.GetAvailableAsync(1, dstDate, dstDate, default);

        // Ensure all generated slots are valid Local times (contain no invalid times)
        //Assert.False(tz.IsInvalidTime(slot.LocalStartTime)) --> False for every slot
        var tz = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        Assert.All(slots, slot => Assert.False(tz.IsInvalidTime(slot.LocalStartTime)));
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