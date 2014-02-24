﻿using Moq;
using Ninject;
using SportsStore.Domain.Abstract;
using SportsStore.Domain.Concrete;
using SportsStore.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace SportsStore.WebUI.Infrastructure
{
	public class NinjectControllerFactory : DefaultControllerFactory
	{
		private IKernel NinjectKernel;

		public NinjectControllerFactory()
		{
			NinjectKernel = new StandardKernel();
			AddBindings();
		}

		protected override IController GetControllerInstance(System.Web.Routing.RequestContext requestContext, Type controllerType)
		{
			return controllerType == null ? null : (IController)NinjectKernel.Get(controllerType);
		}

		private void AddBindings()
		{
			Mock<IProductRepository> mock = new Mock<IProductRepository>();
			mock.Setup(m => m.Products).Returns(new List<Product>()
			{
				new Product { Name = "Football", Price = 25 },
				new Product { Name = "Surf Board", Price = 179 },
				new Product { Name = "Running Shoes", Price = 95 }
			}.AsQueryable());

			//
			// Switch comments if we need to use mock data
			//

			//NinjectKernel.Bind<IProductRepository>().ToConstant(mock.Object);

			NinjectKernel.Bind<IProductRepository>().To<EFProductRepository>();
		}


	}
}