// Starts an animated mascot as a still image, swaps in the video where it's safe, plays it
// a few times, then leaves it stopped on its last frame.
// Only Chromium browsers are trusted to show the WebM's transparency. Others keep the still,
// because a browser that ignores the alpha channel shows a solid green square.
(() => {
    const timesToPlay = 3;
    const isChromium = /\b(Chrome|Chromium|Edg)\//.test(navigator.userAgent)
        && !/\b(iPhone|iPad|iPod)\b/.test(navigator.userAgent);
    const prefersStill = window.matchMedia("(prefers-reduced-motion: reduce)");

    function animate() {
        if (!isChromium || prefersStill.matches) {
            return;
        }

        document.querySelectorAll("img[data-mascot-video]").forEach(img => {
            const video = document.createElement("video");
            video.src = img.dataset.mascotVideo;
            video.className = img.className;
            video.width = img.width;
            video.height = img.height;
            video.muted = true;
            video.autoplay = true;
            video.playsInline = true;
            video.setAttribute("aria-label", img.alt);
            video.addEventListener("canplay", () => img.replaceWith(video), { once: true });

            let timesPlayed = 0;
            video.addEventListener("ended", () => {
                timesPlayed++;
                if (timesPlayed < timesToPlay) {
                    video.play();
                }
            });

            video.load();
        });
    }

    function closeMenu() {
        const toggle = document.getElementById("mk-nav-toggle");
        if (toggle) {
            toggle.checked = false;
        }
    }

    animate();

    if (window.Blazor) {
        Blazor.addEventListener("enhancedload", () => {
            animate();
            closeMenu();
        });
    }
})();
