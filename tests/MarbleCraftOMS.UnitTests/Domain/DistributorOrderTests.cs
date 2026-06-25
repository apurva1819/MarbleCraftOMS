using MarbleCraftOMS.Core.Entities;
using MarbleCraftOMS.Core.Enums;

namespace MarbleCraftOMS.UnitTests.Domain;

public class DistributorOrderTests
{
    [Fact]
    public void Confirm_WhenPending_SetsStatusToConfirmed()
    {
        var order = new DistributorOrder();

        order.Confirm();

        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void Confirm_WhenAlreadyConfirmed_Throws()
    {
        var order = new DistributorOrder { Status = OrderStatus.Confirmed };

        Assert.Throws<InvalidOperationException>(() => order.Confirm());
    }

    [Fact]
    public void Confirm_WhenDispatched_Throws()
    {
        var order = new DistributorOrder { Status = OrderStatus.Dispatched };

        Assert.Throws<InvalidOperationException>(() => order.Confirm());
    }

    [Fact]
    public void Dispatch_WhenConfirmed_SetsStatusToDispatched()
    {
        var order = new DistributorOrder { Status = OrderStatus.Confirmed };

        order.Dispatch();

        Assert.Equal(OrderStatus.Dispatched, order.Status);
    }

    [Fact]
    public void Dispatch_WhenPending_Throws()
    {
        var order = new DistributorOrder(); // default status is Pending

        Assert.Throws<InvalidOperationException>(() => order.Dispatch());
    }

    [Fact]
    public void Cancel_WhenPending_SetsStatusToCancelled()
    {
        var order = new DistributorOrder();

        order.Cancel();

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_WhenConfirmed_SetsStatusToCancelled()
    {
        var order = new DistributorOrder { Status = OrderStatus.Confirmed };

        order.Cancel();

        Assert.Equal(OrderStatus.Cancelled, order.Status);
    }

    [Fact]
    public void Cancel_WhenDispatched_Throws()
    {
        var order = new DistributorOrder { Status = OrderStatus.Dispatched };

        Assert.Throws<InvalidOperationException>(() => order.Cancel());
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_Throws()
    {
        var order = new DistributorOrder { Status = OrderStatus.Cancelled };

        Assert.Throws<InvalidOperationException>(() => order.Cancel());
    }
}
