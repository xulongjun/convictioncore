using ConvictionCore.Domain.SharedKernel;
using ConvictionCore.Domain.Trading;

namespace ConvictionCore.Domain.Tests;

public class ImportBatchTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-17T22:00:00Z", System.Globalization.CultureInfo.InvariantCulture);

    [Fact]
    public void Start_creates_a_running_batch()
    {
        var batch = Start();

        Assert.Equal(ImportBatchStatus.Running, batch.Status);
        Assert.Equal(ImportSource.IbkrFlexActivity, batch.Source);
        Assert.Equal(Now, batch.StartedAt);
        Assert.Null(batch.FinishedAt);
        Assert.Equal(0, batch.Version);
    }

    [Fact]
    public void Succeed_records_counts_and_finishes()
    {
        var batch = Start();

        batch.Succeed(3, 1, 2, "{}", null, Now.AddMinutes(1));

        Assert.Equal(ImportBatchStatus.Succeeded, batch.Status);
        Assert.Equal((3, 1, 2), (batch.InsertedCount, batch.CorrectedCount, batch.UnchangedCount));
        Assert.Equal(Now.AddMinutes(1), batch.FinishedAt);
        Assert.Equal(1, batch.Version);
    }

    [Fact]
    public void Fail_records_code_and_truncates_long_messages()
    {
        var batch = Start();

        batch.Fail("1015", new string('x', ImportBatch.ErrorMessageMaxLength + 50), 0, 0, 0, null, null, Now);

        Assert.Equal(ImportBatchStatus.Failed, batch.Status);
        Assert.Equal("1015", batch.ErrorCode);
        Assert.Equal(ImportBatch.ErrorMessageMaxLength, batch.ErrorMessage!.Length);
    }

    [Fact]
    public void A_finished_batch_cannot_finish_again()
    {
        var batch = Start();
        batch.Succeed(0, 0, 0, null, null, Now);

        Assert.Throws<InvalidOperationException>(() => batch.Fail("x", "y", 0, 0, 0, null, null, Now));
    }

    [Fact]
    public void Start_rejects_an_inverted_window()
    {
        Assert.Throws<InvalidOperationException>(() => ImportBatch.Start(
            ImportBatchId.New(),
            BrokerAccountId.New(),
            ImportSource.IbkrFlexActivity,
            ImportTrigger.Manual,
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 5, 1),
            "parser/1",
            Now));
    }

    private static ImportBatch Start() => ImportBatch.Start(
        ImportBatchId.New(),
        BrokerAccountId.New(),
        ImportSource.IbkrFlexActivity,
        ImportTrigger.Manual,
        new DateOnly(2026, 3, 1),
        new DateOnly(2026, 11, 17),
        "parser/1",
        Now);
}
