for i in range(3):
    if i > 0 and not i == 2:
        move(EAST)
    elif i == 0 or i == 2:
        harvest()
