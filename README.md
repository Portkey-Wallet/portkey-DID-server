# Portkey DID Server

BRANCH | AZURE PIPELINES                                                                                                                                                                                                                                                  | TESTS                                                                                                                                                                                                                        | CODE COVERAGE
-------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|--------------
MASTER | [![Build Status](https://dev.azure.com/Portkey-Finance/Portkey-Finance/_apis/build/status%2FPortkey-Wallet.portkey-DID-server?branchName=master)](https://dev.azure.com/Portkey-Finance/Portkey-Finance/_build/latest?definitionId=9&branchName=master) | [![Test Status](https://img.shields.io/azure-devops/tests/Portkey-Finance/Portkey-Finance/9/master)](https://dev.azure.com/Portkey-Finance/Portkey-Finance/_build/latest?definitionId=9&branchName=master) | [![codecov](https://codecov.io/github/Portkey-Wallet/portkey-DID-server/branch/master/graph/badge.svg?token=2TWAZLIGG8)](https://codecov.io/github/Portkey-Wallet/portkey-DID-server)

Portkey DID Server provides interface services for the Portkey wallet. In terms of project architecture, the project is developed based on the ABP framework. It uses Orleans, which is a framework for building reliable and scalable distributed applications that can simplify the complexity of distributed computing. In terms of data storage, the project uses Grain and Elasticsearch for data storage and retrieval. Grain is the core component of Orleans and represents an automatically scalable and fault-tolerant entity. In summary, Portkey DID Server combines the advantages of the ABP framework, Orleans, and Elasticsearch to achieve a high-performance and scalable distributed wallet interface service.
## Getting Started

Before running Portkey DID Server, you need to prepare the following infrastructure components, as they are essential for the project's operation:
* MongoDB
* Elasticsearch
* Redis
* RabbitMQ

Before running Portkey DID Server, you need to prepare the following infrastructure components, as they are essential for the project's operation:
```Bash
git clone https://github.com/Portkey-Wallet/portkey-DID-server
```

The following command will clone Portkey DID Server into a folder. Please open a terminal and enter the following command:

```Bash
# enter the portkey-DID-server folder and build 
cd portkey-DID-server

# build
dotnet build

# enter CAServer.DbMigrator folder
cd portkey-DID-server/src/CAServer.DbMigrator/bin/Debug/net7.0
# run DbMigrator service
dotnet CAServer.DbMigrator.dll

# enter CAServer.AuthServer folder
cd portkey-DID-server/src/CAServer.AuthServer/bin/Debug/net7.0
# run AuthServer service
dotnet CAServer.AuthServer.dll

# enter CAServer.Silo folder
cd portkey-DID-server/src/CAServer.Silo/bin/Debug/net7.0
# run Silo service
dotnet CAServer.Silo.dll

# enter CAServer.HttpApi.Host folder
cd portkey-DID-server/src/CAServer.HttpApi.Host/bin/Debug/net7.0
# run HttpApi service
dotnet CAServer.HttpApi.Host.dll

# enter CAServer.EntityEventHandler folder
cd portkey-DID-server/src/CAServer.EntityEventHandler/bin/Debug/net7.0
# run EntityEventHandler service
dotnet CAServer.EntityEventHandler.dll

# enter CAServer.ContractEventHandler folder
cd portkey-DID-server/src/CAServer.ContractEventHandler/bin/Debug/net7.0
# run ContractEventHandler service
dotnet CAServer.ContractEventHandler.Core.dll
```

After starting all the above services, Portkey DID Server is ready to provide external services.

## Usage

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
