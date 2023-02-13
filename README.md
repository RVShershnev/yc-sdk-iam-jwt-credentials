# Yandex.Cloud SDK (C#) Iam JWT Token

������ ����� �������� ������������� OAuth Token ��� jwt � ������� ������ BouncyCastle.
������ �� ����������� �����  [Yandex.Cloud SDK](https://github.com/yandex-cloud/dotnet-sdk).  
������ �� ���� � ������������� [��������� IAM-������ ��� ���������� ��������](https://cloud.yandex.ru/docs/iam/operations/iam-token/create-for-sa).

## ���� �� ������ � �������
### ��������� ������

```
dotnet add package YandexCloud.IamJwtCredentials
```

### �������� ������ ��� ���������� ��������

1) ��������� �� �������� ������ ������.
2) �������� ��������� �������.
![�������](docs/CreateAccount.jpg)
3) �������� �������������� ����.
![����](docs/CreateKey.jpg)
4) �������� ���� ���� `authorized_key.json`.

### �����������

������� ���� � ����� `authorized_key.json`

```csharp

IamJwtCredentialsConfiguration configuration = JsonSerializer.Deserialize<IamJwtCredentialsConfiguration>(File.ReadAllText("authorized_key.json"));
var sdk = new Sdk(new IamJwtCredentialsProvider(configuration));

```

### ��������� ������� � ��������

� ������� ���������� ������ `Sdk` ����� �� `grpc` �������� ������ � �������� Yandex Cloud.

```csharp
using Yandex.Cloud;

var sdk = new Sdk();
```
