let App = (function() {
    
    window.TrimEnd = function(str, character) {
        if (!str) return str; 
        let escapedCharacter = character.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
        let regex = new RegExp(escapedCharacter + "+$", 'g');

        return str.replace(regex, '');
    }
    
    let configData = {
        authServer: "",
        clientId: ""
    }
    
    let URI = {
        getConfig : "/api/app/admin/config",
        test : "/api/app/admin/index",
    }

    let AuthServerURI = {
        login : "/connect/token",
    }

    let setupAjax = function() {
        $.ajaxSetup({
            beforeSend: function(xhr) {
                let token = localStorage.getItem("accessToken");
                let tokenType = localStorage.getItem("tokenType");
                if (token) {
                    xhr.setRequestHeader("Authorization", tokenType + " " + token);
                }
            },
            error: function(xhr) {
                try {
                    if (xhr.status === 401){
                        debugger;
                        localStorage.removeItem("accessToken");
                        localStorage.removeItem("tokenType")
                        window.location.href = '/admin/login.html';
                    }
                    console.log(JSON.stringify(xhr));
                    let response = JSON.parse(xhr.responseText);
                    App.showToast("Err_" + xhr.status + ":" + response.error_description);
                } catch (e) {
                    console.error("Error parsing the response: ", e);
                    App.showToast("Err_" + xhr.status + ": " + xhr.statusText);
                }
            }
        });
    };

    let generateDeviceId = function() {
        let deviceId = getCookie("device-id");
        if (deviceId) {
            console.info("Exists DeviceId: " + deviceId)
            return deviceId;
        }
        let userAgent = navigator.userAgent;
        let screenResolution = screen.width + 'x' + screen.height;
        let language = navigator.language;

        deviceId = btoa(userAgent + screenResolution + language);
        console.info("GenerateDeviceId: " + deviceId)
        return deviceId;
    };


    function loadConfig() {
        $.ajax({
            url: URI.getConfig,
            method: 'GET',
            async: false,
            success: function(response) {
                console.info("loadConfig:" + JSON.stringify(response))
                if (response && response.success && response.data) {
                    response.data.authServer = TrimEnd(response.data.authServer, "/")
                    configData = response.data;
                }
            },
            error: function(xhr, status, error) {
                console.error("Error: ", status, error);
            }
        });
    }
    
    let setCookie = function(name, value, seconds) {
        let expires = "";
        if (seconds) {
            let date = new Date();
            date.setTime(date.getTime() + (seconds * 1000));
            expires = "; expires=" + date.toUTCString();
        }
        document.cookie = name + "=" + (value || "")  + expires + "; path=/";
    }

    let getCookie = function(name) {
        let nameEQ = name + "=";
        let ca = document.cookie.split(';');

        for(let i = 0; i < ca.length; i++) {
            let c = ca[i];
            while (c.charAt(0) === ' ') c = c.substring(1, c.length);
            if (c.indexOf(nameEQ) === 0) return c.substring(nameEQ.length, c.length);
        }

        return null;
    };

    function showToast(message) {
        let toast = document.getElementById('toast');
        toast.textContent = message;
        toast.style.display = 'block';
        setTimeout(function() {
            toast.style.display = 'none';
        }, 3000);
    }
    
    $(document).ready(function() {
        setupAjax();
        loadConfig();
        setCookie("device-id", generateDeviceId(), 3600 * 24 * 30);
    });

    return {
        setCookie: setCookie,
        getCookie: getCookie,
        showToast: showToast,
        Config: () => configData,
        Uri: URI,
        AuthServerUri: AuthServerURI,
    };
    
})();