var global = new Object();
global = {
    lastAlertaSyncDate: null,
    getAlertas: function () {
        if(arguments[0]){
            var callback = arguments[0].callback;
        }

        $.getJSON('/Alertas/GetActiveAlertas', function (data) {
            $(data.alertas).each(function () {

                var date = new Date(parseInt(this.fecha.replace("/Date(", "").replace(")/", ""), 10));
                this.dtObj = date;
                this.fechaStr = $.dateformat.date(date.toString(), 'MMMM dd, yyyy @ HH:mm');
            });
            $("#alert-count").html(data.count);
            $("#alerta-notification").html($("#tpl-alert-item-notification").jqote(data.alertas));
            if ($.isFunction(callback)) {
                callback(data);
            }
        });
    }
};
global.getAlertas({ });
setInterval(global.getAlertas, 600000);