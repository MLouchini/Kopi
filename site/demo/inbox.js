"use strict";
const storageKey="kopi-hackathon-leads-v1";
const form=document.querySelector("#lead-form"), saved=document.querySelector("#saved"), error=document.querySelector("#save-error"), rows=document.querySelector("#lead-rows");
function load(){try{const value=JSON.parse(localStorage.getItem(storageKey)||"[]");return Array.isArray(value)?value:[]}catch{return []}}
function render(){const leads=load();rows.replaceChildren();for(const lead of leads){const row=document.createElement("tr");for(const key of ["contact_name","email","project","budget"]){const cell=document.createElement("td");cell.textContent=String(lead[key]||"");row.append(cell)}rows.append(row)}document.querySelector("#lead-table").hidden=leads.length===0;document.querySelector("#empty-state").hidden=leads.length>0}
form.addEventListener("input",()=>{saved.hidden=true;error.hidden=true});
form.addEventListener("submit",event=>{event.preventDefault();saved.hidden=true;error.hidden=true;if(!form.reportValidity())return;const lead=Object.fromEntries(new FormData(form));const leads=load();leads.push(lead);try{localStorage.setItem(storageKey,JSON.stringify(leads))}catch{error.textContent="Could not save: browser storage is unavailable.";error.hidden=false;return}render();form.reset();saved.textContent="Lead saved. The new enquiry is now in your inbox.";saved.hidden=false});
document.querySelector("#clear-leads").addEventListener("click",()=>{localStorage.removeItem(storageKey);saved.hidden=true;error.hidden=true;render()});
render();
