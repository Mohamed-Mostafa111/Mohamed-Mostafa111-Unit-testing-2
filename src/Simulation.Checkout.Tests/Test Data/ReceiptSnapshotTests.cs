using FluentAssertions;
using Moq;
using Simulation.Checkout.Application;
using Simulation.Checkout.Application.Abstractions;
using Simulation.Checkout.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Simulation.Checkout.Tests.Test_Data
{
	public class ReceiptSnapshotTests
	{
		private readonly Mock<IInventoryService> _inventoryService;
		private readonly CheckoutService _checkoutService;


		public ReceiptSnapshotTests(Mock<IInventoryService> inventoryService, CheckoutService checkoutService)
		{
			_inventoryService = inventoryService;
			_checkoutService = checkoutService;
		}

		[Fact]
		public async Task GetSnapShot_whereCheckingOutOrder_ShouldSucceed()
		{
			var order = new Order
			{
				CustomerEmail = "test@example.com",
				CustomerName = "Test Customer",
				Items = new List<OrderItem>
				{
					new OrderItem
					{
						ProductName="TestProduct",
						Quantity=5,
						UnitPrice=50m,
						ProductId= Guid.Parse("11111111-1111-1111-1111-111111111111")
					}
				}
			};
			_inventoryService.Setup(I => I.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>())).ReturnsAsync(true);
			var result = await _checkoutService.CheckoutAsync(order);
			result.Should().NotBeNull();

			var Settings = new VerifySettings();
			Settings.ScrubGuids();
			Settings.ScrubDateTimes();
			Settings.ScrubMember("CreatedAt");
			Settings.ScrubMember("OrderId");
			Settings.ScrubMember("ProductId ");
			await Verify(result, Settings);
		}
	}
}
