using NetArchTest.Rules;
using System;
using System.Collections.Generic;
using System.Text;

namespace Simulation.Checkout.Tests.Test_Data
{
	public class ArchitectureTests
	{
		[Fact]
		public void Domain_Should_Not_Depend_On_Application_Or_Infrastructure()
		{
			var result = Types.InAssembly(typeof(Domain.Order).Assembly)
				.Should()
				.NotHaveDependencyOn("Simulation.Checkout.Application")
				.And()
				.NotHaveDependencyOn("Simulation.Checkout.Infrastructure")
				.GetResult();

			Assert.True(result.IsSuccessful, "Domain layer should not depend on Application or Infrastructure.");
		}
		[Fact]
		public void Application_Should_Not_Depend_On_Infrastructure()
		{
			var result = Types.InAssembly(typeof(Application.CheckoutService).Assembly)
				.Should()
				.NotHaveDependencyOn("Simulation.Checkout.Infrastructure")
				.GetResult();

			Assert.True(result.IsSuccessful, "Application layer should not depend on Infrastructure.");
		}
		[Fact]
		public void All_Interfaces_Should_Start_With_I()
		{
			var result = Types.InAssembly(typeof(Domain.Order).Assembly)
				.That()
				.AreInterfaces()
				.Should()
				.HaveNameStartingWith("I")
				.GetResult();

			Assert.True(result.IsSuccessful, "All interfaces must start with the letter 'I'.");
		}
	}
}
