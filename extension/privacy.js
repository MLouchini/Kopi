globalThis.KopiPageInstance ??= Array.from(crypto.getRandomValues(new Uint8Array(16)),b=>b.toString(16).padStart(2,'0')).join('');
// Shared by observation and execution snapshots. Authentication values are not model evidence.
globalThis.KopiPrivacy={
  authenticationContext(text){return /verification|security\s*code|one[ -]?time|two[ -]?factor|authenticat|sign[ -]?in\s*code|\b2fa\b/i.test(text);},
  text(text,context=''){return this.authenticationContext(context+' '+text)?text.replace(/\b\d{4,8}\b/g,'[temporary code omitted]'):text;},
  value(value,context=''){return /^\d{4,8}$/.test(value.trim())||this.authenticationContext(context)?'[temporary value omitted]':this.text(value,context);}
};
