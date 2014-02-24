﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SportsStore.Domain.Abstract;
using SportsStore.Domain.Entities;
using SportsStore.WebUI.Controllers;
using SportsStore.WebUI.Models;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace SportsStore.UnitTests
{
	[TestClass]
	public class ControllerTests
	{
		private Mock<IProductRepository> GetMockIProductRepository()
		{
			Mock<IProductRepository> mock = new Mock<IProductRepository>();
			mock.Setup(m => m.Products).Returns(new Product[] {
				new Product { ProductID = 1, Name = "P1", Category = "Cat1" },
				new Product { ProductID = 2, Name = "P2", Category = "Cat2" },
				new Product { ProductID = 3, Name = "P3", Category = "Cat1" },
				new Product { ProductID = 4, Name = "P4", Category = "Cat2" },
				new Product { ProductID = 5, Name = "P5", Category = "Cat3" }
			}.AsQueryable());

			return mock;
		}

		#region ProductController

		[TestMethod]
		public void Can_Paginate()
		{
			// Arrange
			Mock<IProductRepository> mock = GetMockIProductRepository();

			ProductController controller = new ProductController(mock.Object);
			controller.PageSize = 3;

			// Act
			ProductsListViewModel result = (ProductsListViewModel)controller.List(null, 2).Model;

			// Assert
			Product[] prodArray = result.Products.ToArray();

			Assert.IsTrue(prodArray.Length == 2);
			Assert.AreEqual(prodArray[0].Name, "P4");
			Assert.AreEqual(prodArray[1].Name, "P5");
		}

		[TestMethod]
		public void Can_Send_Pagination_View_Model()
		{
			// Arrange
			Mock<IProductRepository> mock = GetMockIProductRepository();

			// Arrange
			ProductController controller = new ProductController(mock.Object);
			controller.PageSize = 3;

			// Act
			ProductsListViewModel result = (ProductsListViewModel)controller.List(null, 2).Model;

			// Assert
			PagingInfo pageInfo = result.PagingInfo;
			Assert.AreEqual(pageInfo.CurrentPage, 2);
			Assert.AreEqual(pageInfo.itemsPerPage, 3);
			Assert.AreEqual(pageInfo.TotalItems, 5);
			Assert.AreEqual(pageInfo.TotalPages, 2);
		}

		[TestMethod]
		public void Can_Filter_Products()
		{
			// Arrange
			Mock<IProductRepository> mock = GetMockIProductRepository();

			ProductController controller = new ProductController(mock.Object);
			controller.PageSize = 3;

			// Act
			Product[] result = ((ProductsListViewModel)controller.List("Cat2", 1).Model).Products.ToArray();

			// Assert
			Assert.AreEqual(result.Length, 2);
			Assert.IsTrue(result[0].Name == "P2" && result[0].Category == "Cat2");
			Assert.IsTrue(result[1].Name == "P4" && result[1].Category == "Cat2");
		}

		[TestMethod]
		public void Can_Create_Categories()
		{
			// Arrange
			Mock<IProductRepository> mock = GetMockIProductRepository();

			NavController target = new NavController(mock.Object);

			// Act
			string[] results = ((IEnumerable<string>)target.Menu().Model).ToArray();

			// Assert
			Assert.AreEqual(results.Length, 3);
			Assert.AreEqual(results[0], "Cat1");
			Assert.AreEqual(results[1], "Cat2");
			Assert.AreEqual(results[2], "Cat3");
		}

		[TestMethod]
		public void Indicates_Selected_Category()
		{
			// Arrange
			Mock<IProductRepository> mock = GetMockIProductRepository();

			NavController target = new NavController(mock.Object);

			string categoryToSelect = "Cat2";

			// Act
			string result = target.Menu(categoryToSelect).ViewBag.SelectedCategory;

			// Assert
			Assert.AreEqual(categoryToSelect, result);
		}

		[TestMethod]
		public void Generate_Category_Specific_Product_Count()
		{
			// Arrange
			Mock<IProductRepository> mock = GetMockIProductRepository();

			ProductController target = new ProductController(mock.Object);

			// Act
			int res1 = ((ProductsListViewModel)target.List("Cat1").Model).PagingInfo.TotalItems;
			int res2 = ((ProductsListViewModel)target.List("Cat2").Model).PagingInfo.TotalItems;
			int res3 = ((ProductsListViewModel)target.List("Cat3").Model).PagingInfo.TotalItems;
			int resAll = ((ProductsListViewModel)target.List(null).Model).PagingInfo.TotalItems;

			// Assert
			Assert.AreEqual(res1, 2);
			Assert.AreEqual(res2, 2);
			Assert.AreEqual(res3, 1);
			Assert.AreEqual(resAll, 5);
		}

		#endregion

		#region CartController

		[TestMethod]
		public void Can_Add_To_Cart()
		{
			// Arrange
			Mock<IProductRepository> mock = GetMockIProductRepository();

			Cart cart = new Cart();

			CartController target = new CartController(mock.Object, null);

			// Act
			target.AddToCart(cart, 1, null);

			// Assert
			Assert.AreEqual(cart.Lines.Count(), 1, "Number of Cart Lines Failed");
			Assert.AreEqual(cart.Lines.First().Product.ProductID, 1, "First Cart Line ProductID Failed");
		}

		[TestMethod]
		public void Adding_Product_To_Cart_Goes_To_Cart_Screen()
		{
			// Arrange
			Mock<IProductRepository> mock = GetMockIProductRepository();

			Cart cart = new Cart();

			CartController target = new CartController(mock.Object, null);

			// Act
			RedirectToRouteResult result = target.AddToCart(cart, 2, "myUrl");

			// Assert
			Assert.AreEqual(result.RouteValues["action"], "Index");
			Assert.AreEqual(result.RouteValues["returnUrl"], "myUrl");
		}

		[TestMethod]
		public void Can_View_Cart_Contents()
		{
			// Arrange
			Cart cart = new Cart();

			CartController target = new CartController(null, null);

			// Act
			CartIndexViewModel result = (CartIndexViewModel)target.Index(cart, "myUrl").ViewData.Model;

			// Assert
			Assert.AreSame(result.Cart, cart);
			Assert.AreEqual(result.ReturnUrl, "myUrl");
		}

		[TestMethod]
		public void Cannot_Checkout_Empty_Cart()
		{
			// Arrange
			Mock<IOrderProcessor> mock = new Mock<IOrderProcessor>();

			Cart cart = new Cart();
			ShippingDetails shippingDetails = new ShippingDetails();

			CartController target = new CartController(null, mock.Object);

			// Act
			ViewResult result = target.Checkout(cart, shippingDetails);

			// Assert
			mock.Verify(m => m.ProcessOrder(It.IsAny<Cart>(), It.IsAny<ShippingDetails>()), Times.Never());

			Assert.AreEqual("", result.ViewName);
			Assert.AreEqual(false, result.ViewData.ModelState.IsValid);
		}

		[TestMethod]
		public void Cannot_Checkout_Invalid_ShippingDetails()
		{
			// Arrange
			Mock<IOrderProcessor> mock = new Mock<IOrderProcessor>();

			Cart cart = new Cart();
			cart.AddItem(new Product(), 1);

			CartController target = new CartController(null, mock.Object);
			target.ModelState.AddModelError("error", "error");

			// Act
			ViewResult result = target.Checkout(cart, new ShippingDetails());

			// Assert - check that the order hasn't been passed on to the processor
			mock.Verify(m => m.ProcessOrder(It.IsAny<Cart>(), It.IsAny<ShippingDetails>()), Times.Never());
			
			Assert.AreEqual("", result.ViewName);
			Assert.AreEqual(false, result.ViewData.ModelState.IsValid);
		}

		[TestMethod]
		public void Can_Checkout_And_Submit_Order()
		{
			// Arrange
			Mock<IOrderProcessor> mock = new Mock<IOrderProcessor>();
			
			Cart cart = new Cart();
			cart.AddItem(new Product(), 1);
			
			CartController target = new CartController(null, mock.Object);

			// Act
			ViewResult result = target.Checkout(cart, new ShippingDetails());

			// Assert
			mock.Verify(m => m.ProcessOrder(It.IsAny<Cart>(), It.IsAny<ShippingDetails>()), Times.Once());
			
			Assert.AreEqual("Completed", result.ViewName);
			Assert.AreEqual(true, result.ViewData.ModelState.IsValid);
		}
		#endregion
	}
}
