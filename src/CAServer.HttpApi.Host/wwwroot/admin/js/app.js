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
    
    let user = {
        userId : "",
        userName : "",
        rules : "",
        mfaExists : false
    }
    
    let URI = {
        
        // admin api
        users: "/api/identity/users",
        roles: "/api/identity/roles",
        userById: (id) => "/api/identity/users/" + id,
        userRule: (id) => "/api/identity/users/" + (id ?? user.userId) + "/roles",
        changePassword: "/api/account/my-profile/change-password",
        
        // app api
        config : "/api/app/admin/config",
        user : "/api/app/admin/user",
        mfa : "/api/app/admin/mfa",
        userMfa : "/api/app/admin/user/mfa",
        rampOrders : "/api/app/admin/ramp/orders",
        rampOrder : "/api/app/admin/ramp/order",
        treasuryOrders : "/api/app/admin/treasury/orders",
        treasuryOrder : "/api/app/admin/treasury/order",
        treasuryOrderExport : "/api/app/admin/treasury/order/export",
        treasuryOrderStatusFlow : "/api/app/admin/treasury/order/statusFlow",
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
            contentType: "application/json",
            success: function(response) {
                if (response && response.success) {
                    return alert("success")
                }
                App.showToast(response.message)
            },
            error: function(xhr) {
                try {
                    console.warn(JSON.stringify(xhr));
                    if (xhr.status === 401){
                        logout();
                        return;
                    }
                    let response = JSON.parse(xhr.responseText);
                    if (response && response.error && response.error.message) {
                        App.showToast(response.error.message)
                    } else {
                        App.showToast("Err_" + xhr.status + ":" + response.error_description);
                    }
                } catch (e) {
                    console.error("Error parsing the response: ", e);
                    App.showToast("Err_" + xhr.status + ": " + xhr.statusText);
                }
            }
        });
    };
    
    let logout = function () {
        localStorage.removeItem("accessToken");
        localStorage.removeItem("tokenType");
        window.location.href = '/admin/login.html';
    }
    
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


    let alertUser = function() {
        alert(JSON.stringify(user, null, 4));
    }
    
    let loadConfig = function () {
        $.ajax({
            url: URI.config,
            method: 'GET',
            async: false,
            success: function(response) {
                console.info("loadConfig:" + JSON.stringify(response))
                if (response && response.success && response.data) {
                    response.data.authServer = TrimEnd(response.data.authServer, "/")
                    configData = response.data;
                }
            }
        });
    }
    
    let loadUser = function () {
        $.ajax({
            url: URI.user,
            method: 'GET',
            async: false,
            success: function(response) {
                console.info("loadUser:" + JSON.stringify(response))
                if (!response.success) {
                    return showToast(response.message);
                } 
                if (response && response.success && response.data) {
                    user = response.data;
                }
            }
        });
        clearNoPermissionDom();
    }

    function copyText(fullText) {
        const tempInput = document.createElement('input');
        tempInput.value = fullText;
        document.body.appendChild(tempInput);
        tempInput.select();
        document.execCommand("copy"); 
        document.body.removeChild(tempInput);
        showToast('Copied: ' + fullText);
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

    let getQueryString = function (name) {
        let reg = new RegExp('(^|&)' + name + '=([^&]*)(&|$)', 'i');
        let r = window.location.search.substr(1).match(reg);
        if (r != null) {
            return unescape(r[2]);
        }
        return null;
    };

    let showToast = function (message) {
        let toast = document.getElementById('toast');
        toast.textContent = message;
        toast.style.display = 'block';
        setTimeout(function() {
            toast.style.display = 'none';
        }, 3000);
    }
    
    let clearNoPermissionDom = function() {
        $("[data-role]").each(function() {
            let $element = $(this);
            if (!checkPermission($element.data("role"))) {
                $element.remove();
            }
        });
    }
    
    let checkPermission = function(roles) {
        let expectedRoles = roles.split(",");
        let userRoles = user.rules.split(",")
        return expectedRoles.some(role => userRoles.includes(role));
    }
    
    $(document).ready(function() {
        setupAjax();
        loadConfig();
        $("#user-info span").dblclick(alertUser)
        setCookie("device-id", generateDeviceId(), 3600 * 24 * 30);
    });

    return {
        setCookie: setCookie,
        getCookie: getCookie,
        getQueryString: getQueryString,
        showToast: showToast,
        loadUser: loadUser,
        logout: logout,
        copyText: copyText,
        checkPermission : (permission, url) => checkPermission(permission) || (window.location.href = url ?? "index.html"),
        Config: () => configData,
        User : () => user,
        Uri: URI,
        AuthServerUri: AuthServerURI,
    };
    
})();