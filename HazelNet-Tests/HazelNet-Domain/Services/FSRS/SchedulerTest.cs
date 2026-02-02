using System.Reflection;
using System.Reflection.Metadata;
using HazelNet_Domain.Models;
using HazelNet_Domain.Services.FSRS;
using Xunit; 
using FluentAssertions;

namespace HazelNet_Tests.HazelNet_Domain.Services.FSRS;

public class SchedulerTest
{
    // SUT
    private Scheduler _sched;
    private readonly Card card = new Card() { FrontOfCard = "front", State = State.New };
    
    [Fact]
    public void Scheduler_NewScheduler_ReturnsSchedulerObject()
    {
        // arrange
        // act
        var result = Scheduler.NewScheduler(
            Parameters.DefaultParam(),
            card,
            DateTime.Now,
            s => new BasicScheduler(s)
        );
        // assert
        result.Should().NotBeNull();
        result.Should().BeOfType<Scheduler>();
    }
    
    [Fact]
    public void Scheduler_Preview_ReturnsRecordLog()
    {
        // arrange
        _sched = Scheduler.NewScheduler(
            Parameters.DefaultParam(),
            card,
            DateTime.Now,
            s => new BasicScheduler(s)
        );
        // act
        var result = _sched.Preview();
        // assert
        result.Should().NotBeNull();
        result.Should().BeOfType<RecordLog>();
    }

    [Fact]
    public void Scheduler_Review_ReturnsSchedulingInfo()
    {
        // arrange
        _sched = Scheduler.NewScheduler(
            Parameters.DefaultParam(),
            card,
            DateTime.Now,
            s => new BasicScheduler(s)
        );
        // act
        var result = _sched.Review(Rating.Easy);
        // assert
        result.Should().NotBeNull();
        result.Should().BeOfType<SchedulingInfo>();
    }
}