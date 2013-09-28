jQuery.extend(jQuery.validator.messages, {
    required: "Obligatorio *",
    number: "Formato de numero invalido.",
    digits: "Solo numeros.",
    max: jQuery.validator.format("El valor debe ser menor que {0}."),
    min: jQuery.validator.format("El valor debe ser mayor que {0}.")
});