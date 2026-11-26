ghost-gui-return-to-body-button = Вернуться в тело
ghost-gui-ghost-warp-button = Телепорт призрака
ghost-gui-ghost-roles-button = Роли призраков ({ $count })
ghost-gui-toggle-ghost-visibility-popup-on = Видимость призраков включена.
ghost-gui-toggle-ghost-visibility-popup-off = Видимость призраков выключена.
ghost-gui-toggle-lighting-manager-popup-normal = Обычное освещение.
ghost-gui-toggle-lighting-manager-popup-personal-light = Включено личное освещение.
ghost-gui-toggle-lighting-manager-popup-fullbright = Полная яркость.
ghost-gui-toggle-fov-popup = Поле зрения переключено.

ghost-gui-toggle-hearing-popup-on = Теперь вы слышите все фразы.
ghost-gui-toggle-hearing-popup-off = Теперь вы слышите только радиосвязь и фразы поблизости.

ghost-target-window-title = Телепорт призрака
ghost-target-window-current-button = Телепорт в: { $name }
ghost-target-window-warp-to-most-followed = Телепорт к самому следуемому

ghost-roles-window-title = Роли призраков
ghost-roles-window-available-button = Доступно ({ $rolesCount })
ghost-roles-window-join-raffle-button = Участвовать в лотерее
ghost-roles-window-raffle-in-progress-button =
    Участвовать в лотерее (Осталось { $time }, { $players ->
        [one] { $players } игрок
        [few] { $players } игрока
        *[other] { $players } игроков
    })
ghost-roles-window-leave-raffle-button =
    Покинуть (Осталось { $time }, { $players ->
        [one] { $players } игрок
        [few] { $players } игрока
        *[other] { $players } игроков
    })
ghost-roles-window-request-role-button = Запросить
ghost-roles-window-request-role-button-timer = Запросить ({ $time }сек.)
ghost-roles-window-follow-role-button = Следовать
ghost-roles-window-no-roles-available-label = В настоящее время нет доступных ролей призраков.
ghost-roles-window-rules-footer = Кнопка станет доступна через { $time } секунд (эта задержка нужна, чтобы убедиться, что вы прочитали правила).

ghost-return-to-body-title = Вернуться в тело
ghost-return-to-body-text = Вас воскрешают! Вернуться в своё тело?

## Возрождение (Space Front)

ghost-gui-respawn-button-allowed = Возрождение
ghost-gui-respawn-button-denied = Возрождение ({ $time } с)

## Возрождение: запрет повторных имён

rmc-respawn-name-blocked = Вы уже использовали персонажа «{ $name }» в этом раунде. Выберите другое имя.
rmc-respawn-used-names = Ваш персонаж «{ $name }» закреплён за вами до конца раунда (использовано имён в раунде: { $count }).
