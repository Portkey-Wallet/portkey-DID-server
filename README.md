# Portkey DID Server

BRANCH | AZURE PIPELINES                                                                                                                                                                                                                                                  | TESTS                                                                                                                                                                                                                        | CODE COVERAGE
-------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------
MASTER | [![Build Status](https://dev.azure.com/Portkey-Finance/Portkey-Finance/_apis/build/status%2FPortkey-Wallet.portkey-DID-server?branchName=master)](https://dev.azure.com/Portkey-Finance/Portkey-Finance/_build/latest?definitionId=9&branchName=master) | [![Test Status](https://dev.azure.com/Portkey-Finance/Portkey-Finance/_apis/build/status%2FPortkey-Wallet.portkey-DID-server?branchName=master)](https://dev.azure.com/Portkey-Finance/Portkey-Finance/_build/latest?definitionId=9&branchName=master) | [![codecov](https://codecov.io/github/Portkey-Wallet/portkey-DID-server/branch/master/graph/badge.svg?token=2TWAZLIGG8)](https://codecov.io/github/Portkey-Wallet/portkey-DID-server)


Portkey DID Server provides interface services for the Portkey wallet. In terms of project architecture, the project is developed based on the ABP framework. It uses Orleans, which is a framework for building reliable and scalable distributed applications that can simplify the complexity of distributed computing. In terms of data storage, the project uses Grain and Elasticsearch for data storage and retrieval. Grain is the core component of Orleans and represents an automatically scalable and fault-tolerant entity. In summary, Portkey DID Server combines the advantages of the ABP framework, Orleans, and Elasticsearch to achieve a high-performance and scalable distributed wallet interface service.
## Getting Started

Before running Portkey DID Server, you need to prepare the following infrastructure components, as they are essential for the project's operation:
* MongoDB
* Elasticsearch
* Redis
* RabbitMQ

The following command will clone Portkey DID Server into a folder. Please open a terminal and enter the following command:
```Bash
git clone https://github.com/Portkey-Wallet/portkey-DID-server
```

The next step is to build the project to ensure everything is working correctly. Once everything is built and configuration file is configured correctly, you can run as follows:

```Bash
# enter the portkey-DID-server folder
cd portkey-DID-server

# publish
dotnet publish src/CAServer.DbMigrator/CAServer.DbMigrator.csproj -o portkey/DbMigrator
dotnet publish src/CAServer.AuthServer/CAServer.AuthServer.csproj -o portkey/AuthServer
dotnet publish src/CAServer.Silo/CAServer.Silo.csproj -o portkey/Silo
dotnet publish src/CAServer.HttpApi.Host/CAServer.HttpApi.Host.csproj -o portkey/HttpApi
dotnet publish src/CAServer.EntityEventHandler/CAServer.EntityEventHandler.csproj -o portkey/EntityEventHandler
dotnet publish src/CAServer.ContractEventHandler/CAServer.ContractEventHandler.csproj -o portkey/ContractEventHandler

# enter portkey folder
cd portkey
# ensure that the configuration file is configured correctly

# run DbMigrator service
dotnet DbMigrator/CAServer.DbMigrator.dll

# run AuthServer service
dotnet AuthServer/CAServer.AuthServer.dll

# run Silo service
dotnet Silo/CAServer.Silo.dll

# run HttpApi service
dotnet HttpApi/CAServer.HttpApi.Host.dll

# run EntityEventHandler service
dotnet EntityEventHandler/CAServer.EntityEventHandler.dll

# run ContractEventHandler service
dotnet ContractEventHandler/CAServer.ContractEventHandler.dll
```

After starting all the above services, Portkey DID Server is ready to provide external services.

## Modules

Portkey DID Server includes the following services:

- `CAServer.DbMigrator`: Data initialization service.
- `CAServer.AuthServer`: Authentication service.
- `CAServer.Silo`: Silo service.
- `CAServer.HttpApi.Host`: API interface service.
- `CAServer.EntityEventHandler`: Business event handling service.
- `CAServer.ContractEventHandler`: Contract event handling service.

## Contributing

We welcome contributions to the Portkey DID Server project. If you would like to contribute, please fork the repository and submit a pull request with your changes. Before submitting a pull request, please ensure that your code is well-tested.


## License

Portkey DID Server is licensed under [MIT](https://github.com/Portkey-Wallet/portkey-DID-server/blob/master/LICENSE).

## Contact

If you have any questions or feedback, please feel free to contact us at the Portkey community channels. You can find us on Discord, Telegram, and other social media platforms.

Links:

- Website: https://portkey.finance/
- Twitter: https://twitter.com/Portkey_DID
- Discord: https://discord.com/invite/EUBq3rHQhr
