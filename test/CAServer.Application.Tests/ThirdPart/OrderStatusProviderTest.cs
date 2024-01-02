// using System;
// using System.Threading.Tasks;
// using CAServer.CAActivity;
// using CAServer.Grains.Grain.ThirdPart;
// using CAServer.ThirdPart.Dtos;
// using CAServer.ThirdPart.Provider;
// using Microsoft.Extensions.DependencyInjection;
// using Shouldly;
// using Xunit;
//
// namespace CAServer.ThirdPart;
//
// [Collection(CAServerTestConsts.CollectionDefinitionName)]
// public partial class OrderStatusProviderTest : CAServerApplicationTestBase
// {
//     private readonly IOrderStatusProvider _orderStatusProvider;
//     private readonly IThirdPartOrderAppService _thirdPartOrderAppService;
//
//     public OrderStatusProviderTest()
//     {
//         _orderStatusProvider = GetRequiredService<IOrderStatusProvider>();
//         _thirdPartOrderAppService = GetRequiredService<IThirdPartOrderAppService>();
//     }
//     
//     protected override void AfterAddApplication(IServiceCollection services)
//     {
//         base.AfterAddApplication(services);
//         services.AddSingleton(UserActivityAppServiceTests.GetMockActivityProvider());
//     }
//
//     [Fact]
//     public async Task AddOrderStatusInfoAsync()
//     {
//         await _orderStatusProvider.AddOrderStatusInfoAsync(new OrderStatusInfoGrainDto()
//         {
//             Id = "test",
//             OrderId = Guid.Empty,
//             ThirdPartOrderNo = Guid.NewGuid().ToString(),
//             RawTransaction = "test",
//             OrderStatusInfo = new OrderStatusInfo()
//             {
//                 Status = "Created",
//                 LastModifyTime = DateTime.UtcNow.Microsecond
//             }
//         });
//     }
//
//     [Fact]
//     public async Task UpdateOrderStatus_GetNull_Async()
//     {
//         await _orderStatusProvider.UpdateOrderStatusAsync(new OrderStatusUpdateDto()
//         {
//             OrderId = "test",
//             RawTransaction = "test",
//             Order = new OrderDto
//             {
//                 Id = Guid.Empty,
//                 Status = "Created"
//             }
//         });
//     }
//
//     [Fact]
//     public async Task UpdateOrderStatusAsync()
//     {
//         
//         var orderCreateInput = new CreateUserOrderDto
//         {
//             MerchantName = ThirdPartNameType.Alchemy.ToString(),
//             TransDirect = TransferDirectionType.TokenBuy.ToString()
//         };
//
//         var orderCreatedDto = await _thirdPartOrderAppService.CreateThirdPartOrderAsync(orderCreateInput);
//         orderCreatedDto.Success.ShouldBe(true);
//         
//         var orderId = Guid.Parse(orderCreatedDto.Id);
//         await _orderStatusProvider.AddOrderStatusInfoAsync(new OrderStatusInfoGrainDto()
//         {
//             Id = orderId.ToString(),
//             OrderId = orderId,
//             ThirdPartOrderNo = Guid.NewGuid().ToString(),
//             RawTransaction = "test",
//             OrderStatusInfo = new OrderStatusInfo()
//             {
//                 Status = "Created",
//                 LastModifyTime = DateTime.UtcNow.Microsecond
//             }
//         });
//         await _orderStatusProvider.UpdateOrderStatusAsync(new OrderStatusUpdateDto()
//         {
//             OrderId = orderId.ToString(),
//             RawTransaction = "test",
//             Order = new OrderDto
//             {
//                 Id = orderId,
//                 Status = "Created"
//             }
//         });
//     }
// }