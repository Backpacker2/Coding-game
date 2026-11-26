let steps = 0;
while (steps < 2) {
    steps = steps + 1;
}
for (let i = 0; i < steps + 1; i++) {
    harvest();
    if (i < steps) {
        move(EAST);
    }
}
