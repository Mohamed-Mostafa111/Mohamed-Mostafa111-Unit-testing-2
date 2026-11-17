using AutoFixture;
using Bogus;
using FluentAssertions;
using Moq;
using Simulation.Checkout.Application;
using Simulation.Checkout.Application.Abstractions;
using Simulation.Checkout.Domain;


namespace Simulation.Checkout.Tests.Test_Data
{
	public class TestData
	{
		private readonly Mock<INotificationService> _notificationService;
		private readonly Mock<IOrderRepository> _orderRepository;
		private readonly Mock<IInventoryService> _inventoryService;
		private readonly CheckoutService _checkoutService;

		public TestData()
		{
			_notificationService = new Mock<INotificationService>();
			_orderRepository = new Mock<IOrderRepository>();
			_inventoryService = new Mock<IInventoryService>();
			_checkoutService = new CheckoutService(_orderRepository.Object, _inventoryService.Object, _notificationService.Object);
		}

		[Fact]
		public async Task Checkout_WithAutoFixtureOrder_ShouldSucceed()
		{
			var Fixture = new Fixture();

			Fixture.Customize<OrderItem>(O => O
			.With(O => O.Quantity, () => Fixture.Create<int>() % 10 + 1)
			.With(o => o.UnitPrice, () => (Fixture.Create<decimal>() * 100 + 1)));

			var Order = Fixture.Create<Order>();
			_inventoryService.Setup(I => I.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>())).ReturnsAsync(true);
			var result = await _checkoutService.CheckoutAsync(Order);
			result.Should().NotBeNull();
		}

		[Fact]
		public async Task Checkout_WithBogus_ShouldSucceed()
		{
			Randomizer.Seed = new Random(2026);
			var CustomerFaker = new Faker<Order>()
				.CustomInstantiator(F => new Order
				{
					CustomerEmail = F.Internet.Email(),
					CustomerName = F.Name.FullName(),
					Items = new List<OrderItem>
					{
						new OrderItem{ProductId=new Guid(),ProductName="Product Test",Quantity=5,UnitPrice=100m}
					}
				});
			var Order = CustomerFaker.Generate();
			_inventoryService.Setup(I => I.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>())).ReturnsAsync(true);
			var result = await _checkoutService.CheckoutAsync(Order);
			result.Should().NotBeNull();

		}


	}
}
