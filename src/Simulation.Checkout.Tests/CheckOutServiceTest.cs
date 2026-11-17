using AutoFixture;
using Bogus;
using FluentAssertions;
using Moq;
using Simulation.Checkout.Application;
using Simulation.Checkout.Application.Abstractions;
using Simulation.Checkout.Domain;
using Simulation.Checkout.Domain.Exceptions;
using System.Runtime.Intrinsics.Arm;


namespace Simulation.Checkout.Tests
{
	public class CheckOutServiceTest
	{
		private readonly Mock<INotificationService> _notificationService;
		private readonly Mock<IOrderRepository> _orderRepository;
		private readonly Mock<IInventoryService> _inventoryService;
		private readonly CheckoutService _checkoutService;

		public CheckOutServiceTest()
		{
			_notificationService = new Mock<INotificationService>();
			_orderRepository = new Mock<IOrderRepository>();
			_inventoryService = new Mock<IInventoryService>();
			_checkoutService = new CheckoutService(_orderRepository.Object, _inventoryService.Object, _notificationService.Object);
		}

		public static IEnumerable<object[]> Senarios =>
			new List<object[]>
			{
				new object[]
				{
					new List<OrderItem>
					{
						new OrderItem { ProductId = new Guid(), ProductName = "Test1", Quantity = 2, UnitPrice = 150m },
						new OrderItem { ProductId = new Guid(), ProductName = "Test2", Quantity = 1, UnitPrice = 400m }

					},
					700m,
					700m
				},
				new object[]
				{
					new List<OrderItem>
					{
						new OrderItem { ProductId = new Guid(), ProductName = "Test3", Quantity = 3, UnitPrice = 500m }
					},
					1500m,
					1400m
				},

			};

		//

		[Fact]
		public async Task CheckoutAsync_WhenOrderIsEmpty_ShouldThrowEmptyOrderException()
		{
			var order = new Order
			{
				CustomerEmail = "test@example.com",
				CustomerName = "Test Customer",
				Items = new List<OrderItem>()
			};

			var result = async () => await _checkoutService.CheckoutAsync(order);

			await result.Should().ThrowAsync<EmptyOrderException>();
		}

		[Fact]
		public async Task EnsureInventoryAsync_WhenStockIsEmpty_ShouldThrowInsufficientInventoryException()
		{
			var order = new Order
			{
				CustomerEmail = "test@example.com",
				CustomerName = "Test Customer",
				Items = new List<OrderItem>
				{
					new OrderItem
					{
						ProductName="Mouse",
						Quantity=5,
						UnitPrice=200m,
						ProductId= new Guid()
					}

				}
			};
			var result = async () => await _checkoutService.CheckoutAsync(order);
			await result.Should().ThrowAsync<InsufficientInventoryException>().WithMessage("Insufficient inventory for 'Mouse'.");

		}
		
		[Fact]
		public async Task EnsurePriceAsync_WhenPriceIsLessThanZero_ShouldThrowInvalidPriceException()
		{
			var order = new Order
			{
				CustomerEmail = "test@example.com",
				CustomerName = "Test Customer",
				Items = new List<OrderItem>
				{
					new OrderItem
					{
						ProductName="Keyboard",
						Quantity=5,
						UnitPrice=-150m,
						ProductId= new Guid()
					}

				}
			};

			var result = async () => await _checkoutService.CheckoutAsync(order);
			await result.Should().ThrowAsync<InvalidPriceException>();
		}

		[Fact]
		public async Task EnsurePriceAsync_WhenPriceEqualsZero_Shouldsucceeds()
		{
			var order = new Order
			{
				CustomerEmail = "test@example.com",
				CustomerName = "Test Customer",
				Items = new List<OrderItem>
				{
					new OrderItem
					{
						ProductName="Keyboard",
						Quantity=5,
						UnitPrice=0m,
						ProductId= new Guid()
					}
				}
			};
			_inventoryService.Setup(i => i.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>())).ReturnsAsync(true);
			var result = await _checkoutService.CheckoutAsync(order);
			result.Subtotal.Should().Be(0m);

		}

		[Theory]
		[InlineData(999.99,0)]
		[InlineData(1000,100)]
		[InlineData(1500,100)]
		public async Task EnsureDiscount_WhenSubtotalMoreThan1000_ShouldbeSuceeded(decimal unitPrice,decimal expectedDiscount )
		{
			var order = new Order
			{
				CustomerEmail = "test@example.com",
				CustomerName = "Test Customer",
				Items = new List<OrderItem>
				{
					new OrderItem
					{
						ProductName="Test",
						Quantity=1,
						UnitPrice=unitPrice,
						ProductId= new Guid()
					}
				}
			};
			_inventoryService.Setup(i => i.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>())).ReturnsAsync(true);
			var result =await  _checkoutService.CheckoutAsync(order);
			result.Should().NotBeNull();
			result.Subtotal.Should().Be(unitPrice);
			result.Discount.Should().Be(expectedDiscount);
			result.Total.Should().Be(result.Subtotal - expectedDiscount);
		}


		[Theory]
		[MemberData(nameof(Senarios))]
		public async Task EnsureTotalAndSubtotal_WithinExpectedValues_ShouldbeSuceeded(List<OrderItem>items , decimal Subtotal,decimal Total)
		{
			var order = new Order
			{
				CustomerEmail = "test@example.com",
				CustomerName = "Test Customer",
				Items = items
			};
			_inventoryService.Setup(i => i.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>())).ReturnsAsync(true);
			var result = await _checkoutService.CheckoutAsync(order);
			result.Should().NotBeNull();
			result.Subtotal.Should().Be(Subtotal);
			result.Total.Should().Be(Total);
		}



		[Fact]
		public async Task EnsureOrder_WhereSavingOrder_ShouldSavedTimesOnce_confirmationSentTimesOnce()
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
						UnitPrice=0m,
						ProductId= new Guid()
					}
				}
			};
			_inventoryService.Setup(I => I.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>())).ReturnsAsync(true);
			var result = await _checkoutService.CheckoutAsync(order);
			result.Should().NotBeNull();
			_orderRepository.Verify(O => O.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
			_notificationService.Verify(n => n.SendOrderConfirmationAsync(It.IsAny<CheckoutReceipt>(), It.IsAny<CancellationToken>()), Times.Once);
		}

		[Fact]
		public async Task EnsureInventoryAsync_WhenStockIsEmpty_ShouldExceptionThrownSaveANDSendTimesNever()
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
						UnitPrice=0m,
						ProductId= new Guid()
					}
				}
			};
			_inventoryService.Setup(I => I.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>())).ReturnsAsync(false);
			var result = async () => await _checkoutService.CheckoutAsync(order);
			await result.Should().ThrowAsync<InsufficientInventoryException>();
			_orderRepository.Verify(O => O.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
			_notificationService.Verify(n => n.SendOrderConfirmationAsync(It.IsAny<CheckoutReceipt>(), It.IsAny<CancellationToken>()), Times.Never);

		}
		[Fact]
		public async Task ValidateQuantity_WhenQuantityEqualeZero_ShouldValidationFailAndSaveAndSendAllTimesNever()
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
						Quantity=0,
						UnitPrice=0m,
						ProductId= new Guid()
					}
				}
			};
			_inventoryService.Setup(I => I.IsAvailableAsync(It.IsAny<Guid>(), It.IsAny<int>())).ReturnsAsync(true);
			var result = async () => await _checkoutService.CheckoutAsync(order);
			await result.Should().ThrowAsync<InvalidQuantityException>();
			_orderRepository.Verify(O => O.SaveAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
			_notificationService.Verify(n => n.SendOrderConfirmationAsync(It.IsAny<CheckoutReceipt>(), It.IsAny<CancellationToken>()), Times.Never);

		}


		
	}
}