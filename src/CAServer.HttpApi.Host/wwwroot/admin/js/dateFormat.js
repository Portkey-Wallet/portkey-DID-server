let DateFormat = (function() {

    let defaultFmt = 'yyyy-MM-dd HH:mm:ss';
    let defaultUtcFmt = 'yyyy-MM-ddTHH:mm:ssZ'
    function dateFormat(date, fmt = defaultFmt, useUTC = false) {
        let ret;
        const opt = useUTC ? {
            "y+": date.getUTCFullYear().toString(),
            "M+": (date.getUTCMonth() + 1).toString(),
            "d+": date.getUTCDate().toString(),
            "H+": date.getUTCHours().toString(),
            "m+": date.getUTCMinutes().toString(),
            "s+": date.getUTCSeconds().toString()
        } : {
            "y+": date.getFullYear().toString(),
            "M+": (date.getMonth() + 1).toString(),
            "d+": date.getDate().toString(),
            "H+": date.getHours().toString(),
            "m+": date.getMinutes().toString(),
            "s+": date.getSeconds().toString()
        };

        for (let k in opt) {
            ret = new RegExp("(" + k + ")").exec(fmt);
            if (ret) {
                fmt = fmt.replace(ret[1], (ret[1].length === 1) ? (opt[k]) : (opt[k].padStart(ret[1].length, "0")));
            }
        }
        return fmt;
    }

    return {
        format : dateFormat,
        formatUtc : (date, fmt = defaultUtcFmt) => dateFormat(date, fmt, true)
    };
})();
