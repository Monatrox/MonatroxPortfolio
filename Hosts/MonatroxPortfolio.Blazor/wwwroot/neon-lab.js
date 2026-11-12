// Snapshot the current grids before enabling off-screen rendering, avoiding guessed placeholder heights.
window.prepareNeonLazy = () => {
    for (const grid of document.querySelectorAll('.portfolio-shell .project-grid, .portfolio-shell .skill-groups')) {
        const height = grid.getBoundingClientRect().height;
        if (height > 0) grid.style.setProperty('--neon-intrinsic-height', `${height}px`);
    }
};
