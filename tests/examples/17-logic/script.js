for (let i = 0; i < 3; i++) {
    if (i > 0 && !(i === 2)) {
        move(EAST);
    } else if (i === 0 || i === 2) {
        harvest();
    }
}
