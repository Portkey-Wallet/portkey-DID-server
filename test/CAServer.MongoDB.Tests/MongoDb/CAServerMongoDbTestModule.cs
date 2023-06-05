﻿using System;
using Volo.Abp.Data;
using Volo.Abp.Modularity;

namespace CAServer.MongoDB;

[DependsOn(
    typeof(CAServerTestBaseModule),
    typeof(CAServerMongoDbModule)
    )]
public class CAServerMongoDbTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // var stringArray = CAServerMongoDbFixture.ConnectionString.Split('?');
        // var connectionString = stringArray[0].EnsureEndsWith('/') +
        //                            "Db_" +
        //                        Guid.NewGuid().ToString("N") + "/?" + stringArray[1];
        //
        // Configure<AbpDbConnectionOptions>(options =>
        // {
        //     options.ConnectionStrings.Default = connectionString;
        // });
    }
}
