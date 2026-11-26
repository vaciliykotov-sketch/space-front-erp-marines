### Localization for engine console commands

cmd-hint-float = [float]

## generic command errors

cmd-invalid-arg-number-error = Недопустимое число аргументов.

cmd-parse-failure-integer = { $arg } не является допустимым integer.
cmd-parse-failure-float = { $arg } не является допустимым float.
cmd-parse-failure-bool = { $arg } не является допустимым bool.
cmd-parse-failure-uid = { $arg } не является допустимым UID сущности.
cmd-parse-failure-mapid = { $arg } не является допустимым MapId.
cmd-parse-failure-enum = { $arg } is not a { $enum } Enum.
cmd-parse-failure-grid = { $arg } is not a valid grid.
cmd-parse-failure-cultureinfo = "{ $arg }" is not valid CultureInfo.
cmd-parse-failure-entity-exist = UID { $arg } не соответствует существующей сущности.
cmd-parse-failure-session = There is no session with username: { $username }

cmd-error-file-not-found = Не удалось найти файл: { $file }.
cmd-error-dir-not-found = Не удалось найти директорию: { $dir }.

cmd-failure-no-attached-entity = К этой оболочке не привязана никакая сущность.

## 'help' command
cmd-help-help = Использование: help [имя команды]
    Если имя команды не будет указано, будет выведена общая справка. Если имя команды будет указано, будет выведена справка по этой команде.

cmd-help-no-args = Чтобы получить справку по определённой команде, используйте 'help <command>'. Для получения списка всех доступных команд используйте 'list'. Для поиска по командам используйте 'list <filter>'.
cmd-help-unknown = Неизвестная команда: { $command }
cmd-help-top = { $command } — { $description }
cmd-help-invalid-args = Недопустимое количество аргументов.
cmd-help-arg-cmdname = [имя команды]

## 'cvar' command
cmd-cvar-help = Использование: cvar <name | ?> [значение]
    Если значение предоставлено, оно спарсится и сохранится как новое значение CVar.
    Если нет, отобразится текущее значение CVar.
    Используйте 'cvar ?' для получения списка всех зарегистрированных CVar-ов.

cmd-cvar-invalid-args = Должно быть представлено ровно один или два аргумента.
cmd-cvar-not-registered = CVar '{ $cvar }' не зарегистрирован. Используйте 'cvar ?' для получения списка всех зарегистрированных CVar-ов.
cmd-cvar-parse-error = Входное значение имеет неправильный формат для типа { $type }
cmd-cvar-compl-list = Список доступных CVar-ов
cmd-cvar-arg-name = <name | ?>
cmd-cvar-value-hidden = <value hidden>

## 'cvar_subs' command
cmd-cvar_subs-help = Usage: cvar_subs <name>

cmd-cvar_subs-invalid-args = Must provide exactly one argument.
cmd-cvar_subs-arg-name = <name>

## 'list' command
cmd-list-help = Использование: list [фильтр]
    Выводит список всех доступных команд. Если был предоставлен аргумент, он будет использоваться для фильтрации команд по имени.

cmd-list-heading = SIDE NAME            DESC{ "\u000A" }-------------------------{ "\u000A" }

cmd-list-arg-filter = [фильтр]

## '>' command, aka remote exec
cmd-remoteexec-help = Использование: > <command> [arg] [arg] [arg...]
    Выполняет команду на стороне сервера. Это необходимо, если на клиенте имеется команда с таким же именем, так как при простом выполнении команды сначала будет запущена команда на клиенте.

## 'gc' command
cmd-gc-help = Использование: gc [поколение]
    Использует GC.Collect() для запуска Сборки мусора.
    Если был предоставлен аргумент, то он спарсится как номер поколения GC и используется GC.Collect(int).
    Используйте команду 'gfc' для проведения сборки мусора, со сжатием 'кучи больших объектов' (LOH-compacting).
cmd-gc-failed-parse = Не удалось спарсить аргумент.
cmd-gc-arg-generation = [поколение]

## 'gcf' command
cmd-gcf-help = Использование: gcf
    Выполняет полный GC.Collect(2, GCCollectionMode.Forced, true, true) одновременно сжимая 'кучу больших объектов' LOH.
    Скорее всего, это приведёт к зависанию на сотни миллисекунд, имейте в виду.

## 'gc_mode' command
cmd-gc_mode-help = Использование: gc_mode [тип]
    Если аргумент не был предоставлен, вернётся текущий режим задержки GC.
    Если аргумент был пропущен, он спарсится как GCLatencyMode и будет установлен как режим задержки GC.

cmd-gc_mode-current = текущий режим задержки gc: { $prevMode }
cmd-gc_mode-possible = возможные режимы:
cmd-gc_mode-option = - { $mode }
cmd-gc_mode-unknown = неизвестный режим задержки gc: { $arg }
cmd-gc_mode-attempt = попытка изменения режима задержки gc: { $prevMode } -> { $mode }
cmd-gc_mode-result = полученный режим задержки gc: { $mode }
cmd-gc_mode-arg-type = [тип]

## 'mem' command
cmd-mem-help = Использование: mem

cmd-mem-report = Размер кучи: { TOSTRING($heapSize, "N0") }
    Всего распределено: { TOSTRING($totalAllocated, "N0") }

## 'physics' command
cmd-physics-overlay = { $overlay } не является распознанным оверлеем

## 'lsasm' command
cmd-lsasm-help = Использование: lsasm

## 'exec' command
cmd-exec-help = Использование: exec <fileName>
    Каждая строка в файле выполняется как одна команда, если только она не начинается со знака #

cmd-exec-arg-filename = <fileName>

## 'dump_net_comps' command
cmd-dump_net_comps-help = Использование: dump_net-comps

cmd-dump_net_comps-error-writeable = Регистрация всё ещё доступна для записи, сетевые идентификаторы не были сгенерированы.
cmd-dump_net_comps-header = Регистрации сетевых компонентов:

## 'dump_event_tables' command
cmd-dump_event_tables-help = Использование: dump_event_tables <entityUid>

cmd-dump_event_tables-missing-arg-entity = Отсутствует аргумент сущности
cmd-dump_event_tables-error-entity = Недопустимая сущность
cmd-dump_event_tables-arg-entity = <entityUid>

## 'monitor' command
cmd-monitor-help = Использование: monitor <name>
    Возможные мониторы: { $monitors }
    Вы также можете использовать специальные значения "-all" и "+all", чтобы соответственно скрыть или показать все мониторы.

cmd-monitor-arg-monitor = <monitor>
cmd-monitor-invalid-name = Недопустимое имя монитора
cmd-monitor-arg-count = Отсутствует аргумент монитора
cmd-monitor-minus-all-hint = Скрывает все мониторы
cmd-monitor-plus-all-hint = Показывает все мониторы


## 'setambientlight' command
cmd-set-ambient-light-help = setambientlight [mapid] [r g b a]
cmd-set-ambient-light-parse = Не удалось спарсить аргументы как байтовые значения цветов.

## Mapping commands

cmd-savemap-help = savemap <MapID> <Path> [force]
cmd-savemap-not-exist = Целевая карта не существует.
cmd-savemap-init-warning = Попытка сохранить карту после инициализации без принудительного сохранения.
cmd-savemap-attempt = Попытка сохранить карту { $mapId } в { $path }.
cmd-savemap-success = Карта успешно сохранена.
cmd-savemap-error = Could not save map! See server log for details.
cmd-hint-savemap-id = <MapID>
cmd-hint-savemap-path = <Path>
cmd-hint-savemap-force = [bool]

cmd-loadmap-help = loadmap <MapID> <Path> [x] [y] [rotation] [consistentUids]
cmd-loadmap-nullspace = Невозможно загрузить в карту 0.
cmd-loadmap-exists = Карта { $mapId } уже существует.
cmd-loadmap-success = Карта { $mapId } была загружена из { $path }.
cmd-loadmap-error = При загрузке карты из { $path } произошла ошибка.
cmd-hint-loadmap-x-position = [x-position]
cmd-hint-loadmap-y-position = [y-position]
cmd-hint-loadmap-rotation = [rotation]
cmd-hint-loadmap-uids = [float]

cmd-hint-savebp-id = <Grid EntityID>

## 'flushcookies' command
# Note: the flushcookies command is from Robust.Client.WebView, it's not in the main engine code.

cmd-flushcookies-help = Это гарантирует правильное сохранение файлов cookie на диске в случае неаккуратного выключения.
    Имейте в виду, что фактическая операция является асинхронной.

cmd-ldrsc-help = Использование: ldrsc <path> <type>

cmd-rldrsc-help = Использование: rldrsc <path> <type>

cmd-gridtc-help = Использование: gridtc <gridId>


# Client-side commands
cmd-guidump-help = Использование: guidump

cmd-uitest-help = Использование: uitest

## 'uitest2' command
cmd-uitest2-help = Использование: uitest2 <tab>
cmd-uitest2-arg-tab = <tab>
cmd-uitest2-error-args = Ожидается не более одного аргумента
cmd-uitest2-error-tab = Недопустимая вкладка: '{ $value }'
cmd-uitest2-title = UITest2


cmd-setclipboard-help = Использование: setclipboard <text>

cmd-getclipboard-help = Использование: Getclipboard

cmd-togglelight-help = Использование: togglelight

cmd-togglefov-help = Использование: togglefov

cmd-togglehardfov-help = Использование: togglehardfov

cmd-toggleshadows-help = Использование: toggleshadows

cmd-togglelightbuf-help = Использование: togglelightbuf

cmd-chunkinfo-help = Использование: chunkinfo

cmd-rldshader-help = Использование: rldshader

cmd-cldbglyr-help= Использование: cldbglyr <layer>: Toggle <layer>
    cldbglyr: Отключить все слои

cmd-key-info-help = Использование: keyinfo <Кнопка>

## 'bind' command
cmd-bind-help = Использование: bind { cmd-bind-arg-key } { cmd-bind-arg-mode } { cmd-bind-arg-command }
    Обратите внимание, что это НЕ сохраняет привязки автоматически.
    Используйте команду 'svbind', чтобы сохранить конфигурацию привязки.

cmd-bind-arg-key = <KeyName>
cmd-bind-arg-mode = <BindMode>
cmd-bind-arg-command = <InputCommand>

cmd-net-draw-interp-help = Использование: net_draw_interp

cmd-net-watch-ent-help = Использование: net_watchent <0|EntityUid>

cmd-net-refresh-help = Использование: net_refresh

cmd-net-entity-report-help = Использование: net_entityreport

cmd-fill-help = Заполняет консоль всякой чепухой для отладки.

cmd-cls-help = Очищает консоль отладки от всех сообщений.

cmd-sendgarbage-help = Сервер ответит 'нет ты'.

cmd-loadgrid-help = loadgrid <MapID> <Path> [x y] [вращение] [storeUids]

cmd-loc-help = loc

cmd-tpgrid-help = tpgrid <gridId> <X> <Y> [<MapId>]

cmd-rmgrid-help = rmgrid <gridId>

cmd-mapinit-help = mapinit <mapID>

cmd-lsmap-help = lsmap

cmd-lsgrid-help = lsgrid

cmd-addmap-help = addmap <mapID> [initialize]

cmd-rmmap-help = rmmap <mapId>

cmd-savegrid-help = savegrid <gridID> <Path>

cmd-testbed-help = testbed <mapid> <test>

## 'flushcookies' command
# Note: the flushcookies command is from Robust.Client.WebView, it's not in the main engine code.

## 'addcomp' command
cmd-addcomp-help = addcomp <uid> <componentName>
cmd-addcompc-help = addcompc <uid> <componentName>

## 'rmcomp' command
cmd-rmcomp-help = rmcomp <uid> <componentName>
cmd-rmcompc-help = rmcomp <uid> <componentName>

## 'addview' command
cmd-addview-help = addview <entityUid>
cmd-addviewc-help = addview <entityUid>

## 'removeview' command
cmd-removeview-help = removeview <entityUid>

## 'loglevel' command
cmd-loglevel-help = Использование: loglevel <sawmill> <level>
      sawmill: Метка, которая префиксирует сообщения логов. Именно для него вы устанавливаете уровень.
      level: Уровень логирования. Должно соответствовать одному из значений перечисления LogLevel.

cmd-testlog-help = Использование: testlog <sawmill> <level> <message>
    sawmill: Метка, префиксируемая логированному сообщению.
    level: Уровень логирования. Должно соответствовать одному из значений перечисления LogLevel.
    message: Логируемое сообщение. Заключите в двойные кавычки, если хотите использовать пробелы.

## 'vv' command
cmd-vv-help = Использование: vv <сущность ID|IoC имя интерфейса|SIoC имя интерфейса>

## 'showvelocities' command
cmd-showvelocities-help = Использование: showvelocities

## 'setinputcontext' command
cmd-setinputcontext-help = Использование: setinputcontext <context>

## 'forall' command
cmd-forall-help = Использование: forall <bql query> do <command...>

## 'delete' command
cmd-delete-help = delete <entity UID>

# System commands
cmd-showtime-help = showtime

cmd-restart-help = restart

cmd-shutdown-help = shutdown

cmd-saveconfig-help = saveconfig

cmd-netaudit-help = netaudit

# Player commands
cmd-tp-help = tp <x> <y> [<mapID>]

cmd-tpto-help = tpto <username|uid> [username|uid]...
cmd-tpto-destination-hint = место назначения (uid или имя пользователя)
cmd-tpto-victim-hint = сущность для телепортации (uid или имя пользователя)
cmd-tpto-parse-error = Не удаётся распознать сущность или игрока: { $str }

cmd-listplayers-help = listplayers

cmd-kick-help = kick <PlayerIndex> [<Reason>]

# Spin command
cmd-spin-help = spin velocity [drag] [entityUid]

# Localization command
cmd-rldloc-help = Использование: rldloc

# Debug entity controls
cmd-spawn-help = spawn <прототип> ИЛИ spawn <прототип> <относительная сущность ID> ИЛИ spawn <прототип> <x> <y>
cmd-cspawn-help = cspawn <entity type>

cmd-scale-help = scale <entityUid> <float>

cmd-dumpentities-help = Выводит список объектов с UID и прототипом.

cmd-getcomponentregistration-help = Использование: getcomponentregistration <имя компонента>

cmd-showrays-help = Использование: showrays <raylifetime>

cmd-disconnect-help = Использование: disconnect

cmd-entfo-help = Использование: entfo <entityuid>
    UID сущности может иметь префикс 'c', чтобы быть преобразованной в UID клиентской сущности.

cmd-fuck-help = Вызывает исключение

cmd-showpos-help = Использование: showpos

cmd-showrot-help = Usage: showrot

cmd-showvel-help = Usage: showvel

cmd-showangvel-help = Usage: showangvel

cmd-sggcell-help = Использование: sggcell <gridID> <vector2i>\nЭтот vector2i параметр в форме x<int>,y<int>.

cmd-overrideplayername-help = Использование: overrideplayername <name>

cmd-showanchored-help = Использование: showanchored

cmd-dmetamem-help = Использование: dmetamem <type>

cmd-launchauth-help = Использование: launchauth <account name>

cmd-lightbb-help = Использование: lightbb

cmd-monitorinfo-help = Использование: monitorinfo <id>

cmd-setmonitor-help = Использование: setmonitor <id>

cmd-physics-help = Использование: physics <aabbs / com / contactnormals / contactpoints / distance / joints / shapeinfo / shapes>

cmd-hardquit-help = Убивает игровой клиент мгновенно, не оставляя следов. Не говорит серверу пока.

cmd-quit-help = Правильно завершает работу игрового клиента, уведомляя об этом подключённый сервер и т.д.

cmd-csi-help = Использование: csi

cmd-scsi-help = Использование: scsi

cmd-watch-help = Использование: watch

cmd-showspritebb-help = Использование: showspritebb

cmd-togglelookup-help = Использование: togglelookup

cmd-net_entityreport-help = Использование: net_entityreport

cmd-net_refresh-help = Использование: net_refresh

cmd-net_graph-help = Использование: net_graph

cmd-net_watchent-help = Использование: net_watchent <0|EntityUid>

cmd-net_draw_interp-help = Использование: net_draw_interp <0|EntityUid>

cmd-vram-help = Использование: vram

cmd-showislands-help = Использование: showislands

cmd-showgridnodes-help = Использование: showgridnodes

cmd-profsnap-help = Использование: profsnap

cmd-devwindow-help = Использование: devwindow

cmd-scene-help = Использование: scene <className>

cmd-szr_stats-help = Использование: szr_stats

cmd-hwid-help = Использование: hwid

cmd-vvread-help = Usage: vvread <path>

cmd-vvwrite-help = Использование: vvwrite <path>

cmd-vvinvoke-help = Использование: vvinvoke <path> [arguments...]

cmd-dump_dependency_injectors-help = Использование: dump_dependency_injectors
cmd-dump_dependency_injectors-total-count = Общее количество: { $total }

cmd-dump_netserializer_type_map-help = Использование: dump_netserializer_type_map

cmd-hub_advertise_now-help = Использование: hub_advertise_now

cmd-echo-help = Использование: echo "<сообщение>"

## 'vfs_ls' command
cmd-vfs_ls-help = Использование: vfs_list <path>
    Пример:
    vfs_list /Assemblies

cmd-vfs_ls-err-args = Нужен ровно 1 аргумент.
cmd-vfs_ls-hint-path = <path>

cmd-reloadtiletextures-help = Usage: reloadtiletextures

cmd-audio_length-help = Usage: audio_length { cmd-audio_length-arg-file-name }
cmd-audio_length-arg-file-name = <file name>

## PVS
cmd-pvs-override-info-empty = Entity { $nuid } has no PVS overrides.
cmd-pvs-override-info-global = Entity { $nuid } has a global override.
cmd-pvs-override-info-clients = Entity { $nuid } has a session override for { $clients }.

cmd-localization_set_culture-help = Usage: localization_set_culture <cultureName>
cmd-localization_set_culture-culture-name = <cultureName>
cmd-localization_set_culture-changed = Localization changed to { $code } ({ $nativeName } / { $englishName })
