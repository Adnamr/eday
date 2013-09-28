var totalizacion = new Object();
totalizacion = {
    init: function () {
        $(".timeline").spin("large");
        totalizacion.getContacts();
        totalizacion.updateStatistics();
        $(document).bind('refreshStatsEvent', function () {
            totalizacion.updateStatistics();
        });
        $(document).bind('refreshContacts', function () {
            totalizacion.getContacts();
        });
        //setInterval(totalizacion.updateStatistics, 5000);
    },
    showContact: function (contact, isAlerta) {
        if (isAlerta) {
            $("#timeline-alertas").append($("#tpl-timeline-alertas").jqote(contact));
            $("#alerta-" + contact.MesaId).data("contact", contact);
            $("#timeline-alertas").spin(false);
        } else {
            if (contact.SecondsToCall <= 0) {
                $("#timeline-pendiente").append($("#tpl-timeline-pendientes").jqote(contact));
            } else {
                $("#timeline-pendiente").append($("#tpl-timeline-proximas").jqote(contact));
                $("#contacto-" + contact.MesaId + " .timer").tzineClock({
                    zeroCallback: function (timer) {
                        var item = $(timer).parents(".timelineItem");
                        //var contactExpired = totalizacion.createContactObject(item);
                        item.remove();
                        //$("#timeline-pendiente").append($("#tpl-timeline-pendientes").jqote(contactExpired));
                        $(document).trigger('refreshContacts');
                        $(document).trigger('refreshStatsEvent');

                    }
                });
            }
            $("#contacto-" + contact.MesaId).data("contact", contact);
            $("#timeline-pendiente").spin(false);

        }
    },
    getContacts: function () {
        $.getJSON('/totalizacion/GetContacts', function (data) {
            $(".timeline").html('');
            var contact;
            var i;
            for (i = 0; i < data.proximos.length; i++) {
                contact = data.proximos[i];
                totalizacion.showContact(contact, false);
            }

            for (i = 0; i < data.alertas.length; i++) {
                contact = data.alertas[i];
                totalizacion.showContact(contact, true);
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
    updateTotalizacion: function (item, contacto, values, callback) {
        $.ajax({
            contentType: 'application/json, charset=utf-8',
            type: "POST",
            url: "/totalizacion/UpdateTotalizacion",
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
    updateSingleTotalizacion: function (id, value, callback) {
        $.ajax({
            contentType: 'application/json, charset=utf-8',
            type: "POST",
            url: "/Totalizacion/UpdateSingleTotalizacion",
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
    alertaTotalizacion: function (item, contacto, value, mensaje) {
        $.ajax({
            contentType: 'application/json, charset=utf-8',
            type: "POST",
            url: "/Totalizacion/AlertaTotalizacion",
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
    updateAlertaTotalizacion: function (item, contacto, mensaje) {
        $.ajax({
            contentType: 'application/json, charset=utf-8',
            type: "POST",
            url: "/Totalizacion/ActualizarAlertatotalizacion",
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
    cancelAlertaTotalizacion: function (item, contacto) {
        $.ajax({
            contentType: 'application/json, charset=utf-8',
            type: "POST",
            url: "/totalizacion/CancelarAlertaTotalizacion",
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