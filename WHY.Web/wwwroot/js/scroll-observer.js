// IntersectionObserver for infinite scroll
window.ScrollObserver = {
    observer: null,

    initialize: function (sentinelElement, dotNetHelper) {
        if (this.observer) {
            this.observer.disconnect();
        }

        this.observer = new IntersectionObserver(async (entries) => {
            for (const entry of entries) {
                if (entry.isIntersecting) {
                    await dotNetHelper.invokeMethodAsync('OnSentinelVisible');
                }
            }
        }, {
            rootMargin: '200px'
        });

        if (sentinelElement) {
            this.observer.observe(sentinelElement);
        }
    },

    dispose: function () {
        if (this.observer) {
            this.observer.disconnect();
            this.observer = null;
        }
    }
};
