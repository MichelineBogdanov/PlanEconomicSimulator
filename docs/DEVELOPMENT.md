# Разработка и доставка

## Зафиксированное окружение

Проверка 2026-09-13: Unity Hub, Unity `6000.6.0f1` (`f7f8ed4d1e24`),
Windows Mono и WebGL support, Visual Studio Community 2026 с Unity workload,
.NET SDK x64 `10.0.401`, Git `2.55.0.windows.5`, Git LFS `3.7.1`.
VS Code и его расширения в стандартных каталогах не обнаружены. Visual Studio уже достаточно.
Если используется VS Code, рекомендация расширения записана в `.vscode/extensions.json`;
Unity интегрируется через `com.unity.ide.visualstudio`, устаревший пакет VS Code не добавляем.

В PATH текущей сессии раньше находится x86 dotnet без SDK. Установленный x64 SDK доступен
через `C:\Program Files\dotnet\dotnet.exe`; Unity-тестам внешний dotnet не нужен.
Для будущего `dotnet test` использовать x64 путь или исправить порядок PATH в пользовательском окружении.
Модуль IL2CPP и C++ toolchain не подтверждены; выбранному Windows Mono они не требуются.

Локально подтверждены импорт и компиляция, 2 успешных EditMode-теста, 1 PlayMode-тест,
Windows Mono build и 10-секундный запуск player с Direct3D 12 без обнаруженных ошибок инициализации.
Smoke-процесс после проверки остановлен; длительная стабильность и игровое прохождение не проверялись.
YAML workflow и PowerShell разобраны парсерами; пустые NUnit-прогоны отклоняются валидатором.

Unity-проект находится в корне репозитория. Добавить этот каталог в Hub через Add project.
Не создавать поверх него другой шаблон и не обновлять редактор автоматически.

## Команды

Закрыть редактор этого проекта перед CLI-прогоном. PowerShell из корня:

```powershell
./scripts/Test-Repository.ps1
./scripts/Invoke-Unity.ps1 -Task EditMode
./scripts/Invoke-Unity.ps1 -Task PlayMode
./scripts/Invoke-Unity.ps1 -Task Build
```

Нестандартный путь передаётся через `-UnityPath` или `UNITY_EDITOR_PATH`.
Логи и NUnit XML — `TestResults/<task>/<run-id>/`, exe — `Builds/Windows/`.
Тестовые прогоны проверяют exit code, наличие XML и положительное число успешно выполненных тестов.
PlayMode запускается с графическим устройством. На агенте с ограниченным IPC может понадобиться
разрешение на запуск Unity вне песочницы для связи с Licensing Client.

`-Task Configure` — первоначальная настройка технической сцены и параметров проекта;
обычные сборки и тесты не пересоздают сцену. Текущий каркас не содержит игровой экономики.

## Работа в GitHub

Репозиторий: https://github.com/MichelineBogdanov/PlanEconomicSimulator.
Основная ветка — main. Короткие feat/fix/chore/docs-ветки, PR, Conventional Commits,
review и squash merge владельцем. Автослияние не используется.
Перед работой получать актуальный main; перед отправкой проверять diff, тесты и `.meta`.
Для Git LFS после клонирования выполнить `git lfs install --local`; checkout CI использует LFS.

В первоначально пустом репозитории README создаёт main, затем все изменения идут через PR.
HTTPS-авторизация Git выполняется установленным Git Credential Manager:

```powershell
git credential-manager github login --username MichelineBogdanov --browser
```

В браузере войти в GitHub и подтвердить OAuth-доступ Git Credential Manager.
Это отдельная авторизация от подключённого приложения GitHub. Имя/email автора коммитов
также задаются отдельно и сами по себе не дают права push.

Если GitHub-подключение действует как MichelineBogdanov, пользователь одновременно автор PR:
формальное одобрение собственного PR невозможно. Пока требуется ручное review и merge
владельцем; для технически обязательного approval нужен отдельный автор PR/бот.
CODEOWNERS обозначает владельца, но не заменяет настройки защиты.

Правила защиты main (classic branch protection; эквивалентно можно настроить Rulesets):

- Require a pull request before merging.
- Require status checks: **CI gate**, требовать актуальность ветки относительно main.
- Require conversation resolution; запретить force push и удаление main.
- Required approvals: 0 при единственном аккаунте; 1 и code-owner review после подключения отдельного автора PR.
- Разрешить squash merge; auto-merge не включать.

На 2026-09-13 эти правила включены на GitHub, включая применение ограничений к администратору.
Разрешён squash merge, auto-merge выключен. Изменения находятся в draft PR #1;
самостоятельное слияние агентом не выполнялось.

## CI/CD

`.github/workflows/ci.yml` запускается на PR в main, push в main и вручную:

1. Hosted Ubuntu проверяет структуру, JSON, закреплённую Unity и уникальные GUID ассетов.
2. GameCI запускает EditMode и PlayMode с Unity из ProjectVersion.txt; NUnit XML и логи сохраняются на 14 дней.
3. CI gate требует успешных проверок; пропуск тестов не считается прохождением.
4. Push в main после успешных проверок создаёт Windows x64 Mono-сборку и артефакт с SHA на 14 дней.

Linux CI проверяет переносимость редакторных тестов; работоспособность Windows player дополнительно
проверяем локально. Артефакт Actions не является публикацией в Steam и не получает Steam-секреты.
Actions закреплены на SHA. Кэш Library на первом этапе отключён для простоты диагностики.
После стабильного запуска его можно добавить с ключом OS/Unity/manifest/lock.

Первый облачный запуск подтвердил Repository checks. Оба Unity job остановились на
проверке отсутствующих CI-секретов, до запуска редактора; CI gate корректно завершился ошибкой.

## Активация Unity на CI — отдельный необходимый шаг

Workflow подготовлен для GameCI Personal-схемы с секретами `UNITY_LICENSE`, `UNITY_EMAIL`,
`UNITY_PASSWORD` в Settings → Secrets and variables → Actions.
Значения вводить только в GitHub Secrets, не в чат, YAML, git или PR.
Локальный запуск через Hub сам по себе не подтверждает переносимость лицензии на hosted CI.
При проверке стандартный `C:\ProgramData\Unity\Unity_lic.ulf` отсутствовал.
Не подменять его файлом клиентских токенов и не пытаться извлечь пароль из локального хранилища.

Сначала проверить способ активации для своей лицензии по [GameCI Activation](https://game.ci/docs/github/activation/).
Если Hub выдаёт совместимый `.ulf`, настроить три секрета и проверить первый Actions run.
Если совместимый файл недоступен, текущая схема требует изменения: отдельный лицензированный
runner либо поддерживаемая платная/серверная активация. Лицензию не покупать автоматически.
До первого успешного облачного прогона CI считается подготовленным, но не введённым в эксплуатацию.
Не добавлять локальный рабочий компьютер как публичный self-hosted runner без отдельного решения.

Fork и Dependabot PR не получают секреты; полный Unity-прогон пропускается, CI gate остаётся красным.
Владелец проверяет изменения и переносит их в доверенную ветку для полного прогона.
`pull_request_target` для исполнения чужого кода с секретами не используется.

## Источники

- [VS Code и Unity](https://code.visualstudio.com/docs/other/unity).
- [GameCI Test Runner](https://game.ci/docs/github/test-runner/).
- [GameCI Builder](https://game.ci/docs/github/builder/).
- [GitHub PR reviews](https://docs.github.com/en/pull-requests/reference/pull-request-reviews).
- [GitHub protected branches](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches).
