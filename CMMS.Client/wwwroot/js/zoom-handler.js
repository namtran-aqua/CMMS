window.zoomPanHandler = {
    offsetX: 0,
    offsetY: 0,
    scale: 1,

    enableDrag: function () {
        const img = document.getElementById("zoom-img");

        if (!img) {
            console.warn("Không tìm thấy phần tử ảnh.");
            return;
        }

        let isDragging = false;
        let startX, startY;

        img.style.position = "relative";
        img.style.cursor = "grab";

        const applyTransform = () => {
            img.style.transform = `translate(${window.zoomPanHandler.offsetX}px, ${window.zoomPanHandler.offsetY}px) scale(${window.zoomPanHandler.scale})`;
        };

        img.onmousedown = function (e) {
            isDragging = true;
            startX = e.clientX - window.zoomPanHandler.offsetX;
            startY = e.clientY - window.zoomPanHandler.offsetY;
            img.style.cursor = "grabbing";
        };

        document.onmousemove = function (e) {
            if (!isDragging) return;

            window.zoomPanHandler.offsetX = e.clientX - startX;
            window.zoomPanHandler.offsetY = e.clientY - startY;
            applyTransform();
        };

        document.onmouseup = function () {
            isDragging = false;
            img.style.cursor = "grab";
        };
    },

    zoomIn: function () {
        const img = document.getElementById("zoom-img");
        if (img) {
            window.zoomPanHandler.scale += 0.1;
            console.log("Zoom In - scale:", window.zoomPanHandler.scale);
            img.style.transform = `translate(${window.zoomPanHandler.offsetX}px, ${window.zoomPanHandler.offsetY}px) scale(${window.zoomPanHandler.scale})`;
        }
    },


    zoomOut: function () {
        const img = document.getElementById("zoom-img");
        if (img) {
            window.zoomPanHandler.scale = Math.max(0.1, window.zoomPanHandler.scale - 0.1);
            img.style.transform = `translate(${window.zoomPanHandler.offsetX}px, ${window.zoomPanHandler.offsetY}px) scale(${window.zoomPanHandler.scale})`;
        }
    },

    reset: function () {
        const img = document.getElementById("zoom-img");
        if (img) {
            window.zoomPanHandler.offsetX = 0;
            window.zoomPanHandler.offsetY = 0;
            window.zoomPanHandler.scale = 1;
            img.style.transform = "translate(0px, 0px) scale(1)";
        }
    }
};
