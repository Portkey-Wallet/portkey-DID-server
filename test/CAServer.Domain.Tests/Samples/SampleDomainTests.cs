using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp.Identity;
using Xunit;

namespace CAServer.Samples;

/* This is just an example test class.
 * Normally, you don't test code of the modules you are using
 * (like IdentityUserManager here).
 * Only test your own domain services.
 */
[Collection(CAServerTestConsts.CollectionDefinitionName)]
public class SampleDomainTests : CAServerDomainTestBase
{
    private readonly IIdentityUserRepository _identityUserRepository;
    private readonly IdentityUserManager _identityUserManager;

    public SampleDomainTests()
    {
        _identityUserRepository = GetRequiredService<IIdentityUserRepository>();
        _identityUserManager = GetRequiredService<IdentityUserManager>();
    }

    public class ResponseResultDto<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
    }
    public class VerifierServerResponse
    {
        //VerifierSessionId
        public Guid VerifierSessionId { get; set;}

    }
    [Fact]
    public async Task Should_Set_Email_Of_A_User()
    {
        var str =
            "{\"success\":true,\"message\":null,\"data\":{\"verifierSessionId\":\"88aaa8cc-071c-4fe5-b2d9-e16752641b9f\"}}";
        var memory = new MemoryStream(System.Text.Encoding.Default.GetBytes(str));
        var jsonSerializerOptions =  new JsonSerializerOptions(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        } );
        var a = await JsonSerializer.DeserializeAsync<ResponseResultDto<VerifierServerResponse>>(memory, jsonSerializerOptions);
        
        IdentityUser adminUser;

        /* Need to manually start Unit Of Work because
         * FirstOrDefaultAsync should be executed while db connection / context is available.
         */
        await WithUnitOfWorkAsync(async () =>
        {
            adminUser = await _identityUserRepository
                .FindByNormalizedUserNameAsync("ADMIN");

            await _identityUserManager.SetEmailAsync(adminUser, "newemail@abp.io");
            await _identityUserRepository.UpdateAsync(adminUser);
        });

        adminUser = await _identityUserRepository.FindByNormalizedUserNameAsync("ADMIN");
        adminUser.Email.ShouldBe("newemail@abp.io");
    }
}
