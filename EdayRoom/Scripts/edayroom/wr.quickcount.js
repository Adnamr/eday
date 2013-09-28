var quickcount = new Object();
quickcount = {
    init: function () {
        $(".timeline").spin("large");
        quickcount.getContacts();
        quickcount.updateStatistics();
        $(document).bind('refreshStatsEvent', function () {
            quickcount.updateStatistics();
        });
        $(document).bind('refreshContacts', function () {
            quickcount.getContacts();
        });
        //setInterval(quickcount.updateStatistics, 5000);
    },
    showContact: function (contact, isAlerta) {
        if (isAlerta) {
            $("#timeline-alertas").append($("#tpl-timeline-alertas").jqote(contact));
            $("#alerta-" + contact.CentroId).data("contact", contact);
            $("#timeline-alertas").spin(false);
        } else {
            if (contact.SecondsToCall <= 0) {
                $("#timeline-pendiente").append($("#tpl-timeline-pendientes").jqote(contact));
            } else {
                $("#timeline-pendiente").append($("#tpl-timeline-proximas").jqote(contact));
                $("#contacto-" + contact.CentroId + " .timer").tzineClock({
                    zeroCallback: function (timer) {
                        var item = $(timer).parents(".timelineItem");
                        item.remove();
                        $(document).trigger('refreshContacts');
                        $(document).trigger('refreshStatsEvent');

                    }
                });
            }
            $("#contacto-" + contact.CentroId).data("contact", contact);
            $("#timeline-pendiente").spin(false);

        }
    },
    getContacts: function () {
        $.getJSON('/QuickCount/GetContacts', function (data) {
            $(".timeline").html('');
            var contact;
            var i;
            for (i = 0; i < data.proximos.length; i++) {
                contact = data.proximos[i];
                quickcount.showContact(contact, false);
            }

            for (i = 0; i < data.alertas.length; i++) {
                contact = data.alertas[i];
                quickcount.showContact(contact, true);
            }
            $(".timeline").spin(false);
        });

    },
    sortByTime: function (a, b) {
        return Number($(a).find(".timer").attr("seconds-to-call")) >
            Number($(b).find(".timer").attr("seconds-to-call")) ?
            1 : -1;
    },
    updateStatistics: function () {
        $.getJSON('/Participacion/GetStatistics', function (data) {
            $("#stats-centros").html(data.Centros);
            $("#stats-mesas").html(data.Mesas);
            $("#stats-participacion").html(data.Participacion);
            $("#stats-movilizacion").html(data.Movilizacion);
        });
    },
    createContactObject: function (item) {
        return item.data('contact');
        ;
    },
    updateQuickCount: function (item, contacto, values, callback) {
        console.log(values);
        console.log(contacto);
        $.ajax({
            contentType: 'application/json, charset=utf-8',
            type: "POST",
            url: "/QuickCount/UpdateQuickCount",
            data: JSON.stringify({ contacto: contacto, valores: values }),
            cache: false,
            dataType: "json",
            success: function (contact) {
                $(document).trigger('refreshContacts');
                $(document).trigger('refreshStatsEvent');
                if ($.isFunction(callback)) {
                    callback();
                }
            },
            error: function (x, ts, et) {
                alert('error ' + x + "-" + ts + "-" + et);
            }
        });

    },
    updateSingleQuickCount: function (id, value, callback) {
        $.ajax({
            contentType: 'application/json, charset=utf-8',
            type: "POST",
            url: "/QuickCount/UpdateSingleQuickCount",
            data: JSON.stringify({ totalizacionId: id, value: value }),
            cache: false,
            dataType: "json",
            success: function () {
                if ($.isFunction(callback)) {
                    callback();
                }
            },
            error: function (x, ts, et) {
                alert('error ' + x + "-" + ts + "-" + et);
            }
        });
    },
    alertaQuickCount: function (item, contacto, value, mensaje) {
        $.ajax({
            contentType: 'application/json, charset=utf-8',
            type: "POST",
            url: "/QuickCount/AlertaQuickCount",
            data: JSON.stringify({ contacto: contacto, valor: value, mensaje: mensaje }),
            cache: false,
            dataType: "json",
            success: function (contact) {
                $(document).trigger('refreshContacts');
                $(document).trigger('refreshStatsEvent');
            },
            error: function (x, ts, et) {
                alert('error ' + x + "-" + ts + "-" + et);
            }
        });
    },
    updateAlertaQuickCount: function (item, contacto, mensaje) {
        $.ajax({
            contentType: 'application/json, charset=utf-8',
            type: "POST",
            url: "/QuickCount/ActualizarAlertaquickcount",
            data: JSON.stringify({ contacto: contacto, mensaje: mensaje }),
            cache: false,
            dataType: "json",
            success: function (contact) {
                $(document).trigger('refreshContacts');
            },
            error: function (x, ts, et) {
                alert('error ' + x + "-" + ts + "-" + et);
            }
        });
    },
    cancelAlertaQuickCount: function (item, contacto) {
        $.ajax({
            contentType: 'application/json, charset=utf-8',
            type: "POST",
            url: "/QuickCount/CancelarAlertaQuickCount",
            data: JSON.stringify({ contacto: contacto }),
            cache: false,
            dataType: "json",
            success: function (contact) {
                $(document).trigger('refreshContacts');
                $(document).trigger('refreshStatsEvent');
            },
            error: function (x, ts, et) {
                alert('error ' + x + "-" + ts + "-" + et);
            }
        });
    }
};