rmc-rank-component-examine = { CAPITALIZE(SUBJECT($user)) } { GENDER($user) ->
    [epicene] hold
    *[other] holds
  } the rank of [color=white]{ $rank }[/color].

rmc-playtime-examine = { CAPITALIZE(SUBJECT($user)) } has ({ $hours }) hour{ $hours ->
    [one] { "" }
    *[other] s
} of experience.

rmc-team-examine = { CAPITALIZE(SUBJECT($user)) } has the ({ $team }) squad badge.
