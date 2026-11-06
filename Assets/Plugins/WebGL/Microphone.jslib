mergeInto(LibraryManager.library, {
    StartWebGLMicrophone: function () {
        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
            console.error("Microphone API not supported on this browser.");
            return;
        }

        window.audioChunks = [];
        navigator.mediaDevices.getUserMedia({ audio: true })
            .then(function (stream) {
                var audioContext = new (window.AudioContext || window.webkitAudioContext)({ sampleRate: 16000 });
                var source = audioContext.createMediaStreamSource(stream);
                var processor = audioContext.createScriptProcessor(4096, 1, 1);

                window.localAudioStream = stream;
                window.localAudioContext = audioContext;
                window.localAudioProcessor = processor;

                processor.onaudioprocess = function (e) {
                    var inputData = e.inputBuffer.getChannelData(0);
                    // Append raw float32 samples directly
                    window.audioChunks.push(new Float32Array(inputData));
                };

                source.connect(processor);
                processor.connect(audioContext.destination);
                console.log("~ WebGL Microphone Recording Started...");
            })
            .catch(function (err) {
                console.error("Mic access denied: ", err);
            });
    },

    StopWebGLMicrophone: function () {
        if (!window.localAudioProcessor) return;

        // Disconnect audio nodes
        window.localAudioProcessor.disconnect();
        if (window.localAudioContext) window.localAudioContext.close();
        if (window.localAudioStream) {
            window.localAudioStream.getTracks().forEach(track => track.stop());
        }

        // Flatten the multi-chunk array into a unified single array
        var totalLength = window.audioChunks.reduce((acc, chunk) => acc + chunk.length, 0);
        var unifiedArray = new Float32Array(totalLength);
        var offset = 0;
        for (var i = 0; i < window.audioChunks.length; i++) {
            unifiedArray.set(window.audioChunks[i], offset);
            offset += window.audioChunks[i].length;
        }

        console.log("~ Recording stopped. Processing " + totalLength + " audio samples.");

        // Convert float array to a comma-separated string string matching your pipeline target format
        var audioStringArray = unifiedArray.join(",");
        
        // Pass directly to the window handler running in your index.html
        if (typeof window.onUnityAudioReceived === "function") {
            window.onUnityAudioReceived(audioStringArray);
        } else {
            console.error("window.onUnityAudioReceived handler not found!");
        }
    }
});
