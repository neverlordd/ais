# ShiftLine

ASP.NET Core MVC приложение учета рабочего времени сотрудников.

## Структура

- Корень репозитория: `/Users/neverlordd/AIS`
- Сам проект .NET: [AIS/AIS.csproj](./AIS/AIS.csproj)

## Запуск из терминала

Из корня проекта:

```bash
npm run dev
```

Альтернативы:

```bash
npm run start
npm run build
```

## Запуск из VS Code

1. Откройте папку `/Users/neverlordd/AIS` в VS Code.
2. Перейдите в `Run and Debug`.
3. Выберите конфигурацию `Launch ShiftLine`.
4. Нажмите `F5`.

VS Code использует:

- [`.vscode/launch.json`](./.vscode/launch.json)
- [`.vscode/tasks.json`](./.vscode/tasks.json)

## Адрес приложения

После запуска приложение поднимается по адресу:

```text
http://localhost:5214
```
