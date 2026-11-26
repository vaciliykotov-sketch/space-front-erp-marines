rmc-rank-component-examine = { CAPITALIZE(SUBJECT($user)) } имеет ранг [color=white]{ $rank }[/color].

rmc-playtime-examine = { CAPITALIZE(SUBJECT($user)) } имеет опыт ({ $hours }) { $hours ->
    [one] час
    [few] часа
    *[other] часов
}.

rmc-team-examine = { CAPITALIZE(SUBJECT($user)) } имеет знак ({ $team }) отряд.
