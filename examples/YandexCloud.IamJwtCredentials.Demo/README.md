### Тестирование фреймворков
Чтобы быстро проверить работоспособность библиотеки используйте следующие переменные среды
```dotenv
DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
LD_LIBRARY_PATH=$LD_LIBRARY_PATH:./libs/net2.0/
OPENSSL_CONF=/dev/null
```
Проверено для `YandexCloud.IamJwtCredentials`:
- `netstandard2.0` (`netcoreapp3.1`)
- `netstandard2.1` (`netcoreapp3.1`, `net5.0`, `net8.0`)
- `net5.0`