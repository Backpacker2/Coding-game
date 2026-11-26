steps = 0
while steps < 2:
    steps = steps + 1
for i in range(steps + 1):
    harvest()
    if i < steps:
        move(EAST)
