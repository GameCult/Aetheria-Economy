
INCLUDE TestLociA.ink
INCLUDE TestLociB.ink
INCLUDE TestLociC.ink
INCLUDE TestLociD.ink

LIST brunch_state = (no_food), pepper, egg, potatoes, bacon, cooking_utensils, fork, kitchen_usage

LIST hunger_state = not_hungry, (mild), moderate, very, will_eat_spacehorse

LIST pepper_state = (nope), yes

LIST loopstate = (one), two, three, many, lots

VAR loopnum = ()

===function get(x)
~ brunch_state += x

===function statedetermination()
//placeholder function for external state check of loop
~ loopnum = lots




