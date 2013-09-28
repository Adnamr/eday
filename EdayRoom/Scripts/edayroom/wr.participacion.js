var participacion = new Object();
participacion = {
    init: function () {
        $(".timeline").spin("large");
        participacion.getContacts();
        participacion.updateStatistics();

        $(document).bind('refreshStatsEvent', function () {
            participacion.updateStatistics();
        });
        $(document).bind('refreshContacts', function () {
            participacion.getContacts();
        });
    },
    
    showContact: function (contact, isAlerta) {
        if (isAlerta) {
            $("#timeline-alertas").append($("#tpl-timeline-alertas").jqote(contact));
            $("#alerta-" + contact.IdTestigo).data("contact", contact);
            $("#timeline-alertas").spin(false);

        } else {
            if (contact.SecondsToCall <= 0) {
                $("#timeline-pendiente").append($("#tpl-timeline-pendientes").jqote(contact));
            } else {
                $("#timeline-pendiente").append($("#tpl-timeline-proximas").jqote(contact));
                $("#contacto-" + contact.IdTestigo + " .timer").tzineClock({
                    zeroCallback: function (timer) {
                        var item = $(timer).parents(".timelineItem");
                        var contactExpired = participacion.createContactObject(item);
                        item.remove();
                        $("#timeline-pendiente").append($("#tpl-timeline-pendientes").jqote(contactExpired));
                    }
                });
            }
            $("#contacto-" + contact.IdTestigo).data("contact", contact);
            $("#timeline-pendiente").spin(false);

        }
    },
    getContacts: function () {
        var rand = Math.random();
            $.getJSON('/Participacion/GetContacts?rand='+rand, function (data) {
           
            $(".timeline").html('');
            var contact;
            var i;
            for (i = 0; i < data.proximos.length; i++) {
                contact = data.proximos[i];
                participacion.showContact(contact, false);
            }

            for (i = 0; i < data.alertas.length; i++) {
                contact = data.alertas[i];
                participacion.showContact(contact, true);
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
            console.log(data);
            $("#stats-centros").html(data.Centros);
            $("#stats-mesas").html(data.Mesas);
            $("#stats-participacion").html(data.Participacion);
            $("#stats-movilizacion").html(data.Movilizacion);
        });
    },
    createContactObject: function (item) {
        return item.data('contact');
    },
    updateParticipacion: function (item, contacto, value, cola, callback) {
        $.ajax({
            contentType: 'application/json, charset=utf-8',
            type: "POST",
            url: "/Participacion/UpdateParticipacion",
            data: JSON.stringify({ contacto: contacto, valor: value, cola: cola }),
            cache: false,
            dataType: "json",
            success: function () {
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
    updateSingleParticipacion: function (id, value, cola, callback) {
        $.ajax({
            contentType: 'application/json, charset=utf-8',
            type: "POST",
            url: "/Participacion/UpdateSingleParticipacion",
            data: JSON.stringify({ participacionId: id, valor: value, cola: cola }),
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
    updateLastParticipacion: function (item, contacto, value, cola) {
        $.ajax({
            contentType: 'application/json, charset=utf-8',
            type: "POST",
            url: "/Participacion/UpdateLastParticipacion",
            data: JSON.stringify({ contacto: contacto, valor: value, cola: cola }),
            cache: false,
            dataType: "json",
            success: function () {
                $(document).trigger('refreshContacts');
                $(document).trigger('refreshStatsEvent');
            },
            error: function (x, ts, et) {
                alert('error ' + x + "-" + ts + "-" + et);
            }
        });
    },
    alertaPartipacion: function (item, contacto, value, mensaje) {
        $.ajax({
            contentType: 'application/json, charset=utf-8',
            type: "POST",
            url: "/Participacion/AlertaParticipacion",
            data: JSON.stringify({ contacto: contacto, valor: value, mensaje: mensaje }),
            cache: false,
            dataType: "json",
            success: function () {
                $(document).trigger('refreshContacts');
                $(document).trigger('refreshStatsEvent');
            },
            error: function (x, ts, et) {
                alert('error ' + x + "-" + ts + "-" + et);
            }
        });
    },
    updateAlertaPartipacion: function (item, contacto, mensaje) {
        $.ajax({
            contentType: 'application/json, charset=utf-8',
            type: "POST",
            url: "/Participacion/ActualizarAlertaParticipacion",
            data: JSON.stringify({ contacto: contacto, mensaje: mensaje }),
            cache: false,
            dataType: "json",
            success: function () {
                $(document).trigger('refreshContacts');
            },
            error: function (x, ts, et) {
                alert('error ' + x + "-" + ts + "-" + et);
            }
        });
    },
    cancelAlertaParticipacion: function (item, contacto) {
        $.ajax({
            contentType: 'application/json, charset=utf-8',
            type: "POST",
            url: "/Participacion/CancelarAlertaParticipacion",
            data: JSON.stringify({ contacto: contacto }),
            cache: false,
            dataType: "json",
            success: function () {
                $(document).trigger('refreshContacts');
                $(document).trigger('refreshStatsEvent');
            },
            error: function (x, ts, et) {
                alert('error ' + x + "-" + ts + "-" + et);
            }
        });
    },
    cerrarMesa: function (contacto, callback) {
        $.ajax({
            contentType: 'application/json, charset=utf-8',
            type: "POST",
            url: "/Participacion/CerrarMesa",
            data: JSON.stringify({ contacto: contacto}),
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
    }
};