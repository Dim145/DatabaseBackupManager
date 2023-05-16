function onCronChange()
{
    let value = this.value;
    let cronText;

    try {
        cronText = cronstrue.toString(value);
        
        this.setCustomValidity("");
    }
    catch (e) {
        cronText = "Invalid Cron Expression";
        
        this.setCustomValidity(cronText);
    }

    $('#cron-description').text(cronText);
}