# Yandex.Cloud SDK (C#) Iam JWT Token

Данный пакет является альтернативой OAuth Token или jwt с помощью пакета BouncyCastle.
Ссылка на официальный пакет  [Yandex.Cloud SDK](https://github.com/yandex-cloud/dotnet-sdk).  
Ссылка на сайт с документацией [Получение IAM-токена для сервисного аккаунта](https://cloud.yandex.ru/docs/iam/operations/iam-token/create-for-sa).

## Гайд по работе с пакетом
### Установка пакета

```
dotnet add package YandexCloud.IamJwtCredentials
```

### Создание ключей для сервисного аккаунта

1) Перейдите на страницу вашего облака.
2) Создайте сервисный аккаунт.
![Аккаунт](docs/CreateAccount.jpg)
3) Создайте авторизованный ключ.
![Ключ](docs/CreateKey.jpg)
4) Скачайте ключ файл `authorized_key.json`.

### Подключение

Укажите путь к файлу `authorized_key.json`

```csharp

IamJwtCredentialsConfiguration configuration = JsonSerializer.Deserialize<IamJwtCredentialsConfiguration>(File.ReadAllText("authorized_key.json"));
var sdk = new Sdk(new IamJwtCredentialsProvider(configuration));

```

### Получение доступа к сервисам

С помощью экземпляра класса `Sdk` можно по `grpc` получить доступ к сервисам Yandex Cloud.

```csharp
using Yandex.Cloud;

var sdk = new Sdk();
```
