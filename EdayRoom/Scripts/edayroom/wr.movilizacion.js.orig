var movilizacion = new Object();
movilizacion = {
    init: function() {
        $(".timeline").spin("large");
        movilizacion.getContacts();
        movilizacion.updateStatistics();

        $(document).bind('refreshStatsEvent', function() {
            movilizacion.updateStatistics();
        });
        $(document).bind('refreshContacts', function() {
            movilizacion.getContacts();
        });

        //setInterval(movilizacion.updateStatistics, 5000);
    },
    showContact: function(contact, isAlerta) {
        if (isAlerta) {
            $("#timeline-alertas").append($("#tpl-timeline-alertas").jqote(contact));
            $("#alerta-" + contact.IdCentro).data("contact", contact);
            $("#timeline-alertas").spin(false);

        } else {
            if (contact.SecondsToCall <= 0) {
                $("#timeline-pendiente").append($("#tpl-timeline-pendientes").jqote(contact));
            } else {
                $("#timeline-pendiente").append($("#tpl-timeline-proximas").jqote(contact));
                $("#contacto-" + contact.IdCentro + " .timer").tzineClock({
                    zeroCallback: function(timer) {
                        var item = $(timer).parents(".timelineItem");
                        var contactExpired = movilizacion.createContactObject(item);
                        item.remove();
                        $("#timeline-pendiente").append($("#tpl-timeline-pendientes").jqote(contactExpired));
                    }
                });
            }
            $("#contacto-" + contact.IdCentro).data("contact", contact);
            $("#timeline-pendiente").spin(false);

        }
    },
    getContacts: function() {
        $.getJSON('/Movilizacion/GetContacts', function(data) {
            $(".timeline").html('');
            var contact;
            var i;
            for (i = 0; i < data.proximos.length; i++) {
                contact = data.proximos[i];
                movilizacion.showContact(contact, false);
            }

            for (i = 0; i < data.alertas.length; i++) {
                contact = data.alertas[i];
                movilizacion.showContact(contact, true);
            }
            $(".timeline").spin(false);
        });

    },
    sortByTime: function(a, b) {
        return Number($(a).find(".timer").attr("seconds-to-call")) >
            Number($(b).find(".timer").attr("seconds-to-call")) ?
            1 : -1;
    },
    updateStatistics: function() {
        $.getJSON('/Movilizacion/GetStatistics', function(data) {
            $("#stats-centros").html(data.Centros);
            $("#stats-mesas").html(data.Mesas);
            $("#stats-movilizacion").html(data.Movilizacion);
            $("#stats-movilizacion").html(data.Movilizacion);
        });
    },
    createContactObject: function(item) {
        return item.data('contact');
        ;
    },
    updateMovilizacion: function(item, contacto, value, callback) {
        $.ajax({
            contentType: 'application/json, charset=utf-8',
            type: "POST",
            url: "/Movilizacion/UpdateMovilizacion",
            data: JSON.stringify({ contacto: contacto, valor: value }),
            cache: false,
            dataType: "json",
            success: function(contact) {
                $(document).trigger('refreshContacts');
                $(document).trigger('refreshStatsEvent');
                if ($.isFunction(callback)) {
                    callback(contact);
                }
            },
            error: function(x, ts, et) {
                alert('error ' + x + "-" + ts + "-" + et);
            }
        });

    },
    updateSingleMovilizacion: function(id, value, callback) {
        $.ajax({
            contentType: 'application/json, charset=utf-8',
            type: "POST",
            url: "/Movilizacion/UpdateSingleMovilizacion",
            data: JSON.stringify({ movilizacionId: id, valor: value }),
            cache: false,
            dataType: "json",
            success: function() {
                if ($.isFunction(callback)) {
                    callback();
                }
            },
            error: function(x, ts, et) {
                alert('error ' + x + "-" + ts + "-" + et);
            }
        });
    },
    updateLastMovilizacion: function(item, contacto, value, cola) {
        $.ajax({
            contentType: 'application/json, charset=utf-8',
            type: "POST",
            url: "/Movilizacion/UpdateLastMovilizacion",
            data: JSON.stringify({ contacto: contacto, valor: value, cola: cola }),
            cache: false,
            dataType: "json",
            success: function(contact) {
                $(document).trigger('refreshContacts');
                $(document).trigger('refreshStatsEvent');
            },
            error: function(x, ts, et) {
                alert('error ' + x + "-" + ts + "-" + et);
            }
        });
    },
    alertaPartipacion: function(item, contacto, value, mensaje) {
        $.ajax({
            contentType: 'application/json, charset=utf-8',
            type: "POST",
            url: "/Movilizacion/AlertaMovilizacion",
            data: JSON.stringify({ contacto: contacto, valor: value, mensaje: mensaje }),
            cache: false,
            dataType: "json",
            success: function(contact) {
                $(document).trigger('refreshContacts');
                $(document).trigger('refreshStatsEvent');
            },
            error: function(x, ts, et) {
                alert('error ' + x + "-" + ts + "-" + et);
            }
        });
    },
    updateAlertaPartipacion: function(item, contacto, mensaje) {
        $.ajax({
            contentType: 'application/json, charset=utf-8',
            type: "POST",
            url: "/Movilizacion/ActualizarAlertaMovilizacion",
            data: JSON.stringify({ contacto: contacto, mensaje: mensaje }),
            cache: false,
            dataType: "json",
            success: function(contact) {
                $(document).trigger('refreshContacts');
            },
            error: function(x, ts, et) {
                alert('error ' + x + "-" + ts + "-" + et);
            }
        });
    },
    cancelAlertaMovilizacion: function(item, contacto) {
        $.ajax({
            contentType: 'application/json, charset=utf-8',
            type: "POST",
            url: "/Movilizacion/CancelarAlertaMovilizacion",
            data: JSON.stringify({ contacto: contacto }),
            cache: false,
            dataType: "json",
            success: function(contact) {
                $(document).trigger('refreshContacts');
                $(document).trigger('refreshStatsEvent');
            },
            error: function(x, ts, et) {
                alert('error ' + x + "-" + ts + "-" + et);
            }
        });
    }
};