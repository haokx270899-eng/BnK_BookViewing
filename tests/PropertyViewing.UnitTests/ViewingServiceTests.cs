using Microsoft.Extensions.Logging.Abstractions;
using PropertyViewing.Application.DTOs;
using PropertyViewing.Application.Exceptions;
using PropertyViewing.Application.Interfaces;
using PropertyViewing.Application.Services;
using PropertyViewing.Domain.Entities;
using Xunit;

namespace PropertyViewing.UnitTests;

public sealed class ViewingServiceTests
{
    [Fact] public async Task BookAsync_ValidAlignedSlot_CreatesViewing()
    {
        var repository = new FakeRepository(); var service = CreateService(repository);
        var result = await service.BookAsync(new(1, 1, At(10, 30)), default);
        Assert.Equal(At(11, 0), result.EndTime); Assert.Single(repository.Viewings);
    }
    [Fact] public async Task BookAsync_AlreadyBookedSlot_ThrowsConflict()
    {
        var repository = new FakeRepository(); await repository.CreateAsync(NewViewing(1, At(10, 30)), default);
        await Assert.ThrowsAsync<BookingConflictException>(() => CreateService(repository).BookAsync(new(1, 2, At(10, 30)), default));
    }
    [Theory] [InlineData(8, 30)] [InlineData(20, 0)] [InlineData(10, 15)]
    public async Task BookAsync_InvalidHoursOrAlignment_IsRejected(int hour, int minute) => await Assert.ThrowsAsync<ValidationException>(() => CreateService(new FakeRepository()).BookAsync(new(1, 1, At(hour, minute)), default));
    [Fact] public async Task BookAsync_NonExistentProperty_IsNotFound()
    {
        var repository = new FakeRepository { Properties = [] };
        await Assert.ThrowsAsync<NotFoundException>(() => CreateService(repository).BookAsync(new(99, 1, At(10, 0)), default));
    }
    [Fact] public async Task BookAsync_NonExistentUser_IsNotFound()
    {
        var repository = new FakeRepository { Users = [] };
        await Assert.ThrowsAsync<NotFoundException>(() => CreateService(repository).BookAsync(new(1, 99, At(10, 0)), default));
    }
    [Fact] public async Task AvailableAsync_ExcludesBookedSlot()
    {
        var repository = new FakeRepository(); await repository.CreateAsync(NewViewing(1, At(10, 30)), default);
        var slots = await CreateService(repository).GetAvailableAsync(1, DateOnly.FromDateTime(At(0, 0)), DateOnly.FromDateTime(At(0, 0)), default);
        Assert.DoesNotContain(slots, x => x.StartTime == At(10, 30)); Assert.Contains(slots, x => x.StartTime == At(10, 0));
    }
    [Fact] public async Task AvailableAsync_MultipleDays_ReturnsSlotsForEachDay()
    {
        var service = CreateService(new FakeRepository()); var date = DateOnly.FromDateTime(At(0, 0));
        var slots = await service.GetAvailableAsync(1, date, date.AddDays(1), default);
        Assert.Equal(44, slots.Count);
    }
    private static ViewingService CreateService(FakeRepository repository) => new(repository, NullLogger<ViewingService>.Instance);
    private static DateTime At(int hour, int minute) => new(2026, 9, 15, hour, minute, 0);
    private static Viewing NewViewing(int propertyId, DateTime start) => new() { PropertyId = propertyId, UserId = 1, StartTime = start, EndTime = start.AddMinutes(30), CreatedAt = DateTime.UtcNow };
}

public sealed class FakeRepository : IViewingRepository
{
    public HashSet<int> Properties { get; set; } = [1, 2, 3];
    public HashSet<int> Users { get; set; } = [1, 2, 3];
    public List<Viewing> Viewings { get; } = [];
    public Task<bool> PropertyExistsAsync(int id, CancellationToken token) => Task.FromResult(Properties.Contains(id));
    public Task<bool> UserExistsAsync(int id, CancellationToken token) => Task.FromResult(Users.Contains(id));
    public Task<bool> HasConflictAsync(int propertyId, DateTime start, DateTime end, CancellationToken token) => Task.FromResult(Viewings.Any(x => x.PropertyId == propertyId && x.StartTime < end && x.EndTime > start));
    public Task<IReadOnlyList<Viewing>> GetByPropertyAndDateRangeAsync(int propertyId, DateTime from, DateTime to, CancellationToken token) => Task.FromResult<IReadOnlyList<Viewing>>(Viewings.Where(x => x.PropertyId == propertyId && x.StartTime < to && x.EndTime > from).ToList());
    public Task<Viewing> CreateAsync(Viewing viewing, CancellationToken token) { viewing.Id = Viewings.Count + 1; Viewings.Add(viewing); return Task.FromResult(viewing); }
}
