import mongoose from 'mongoose';
import bcrypt from 'bcrypt';

const UsuarioSchema = new mongoose.Schema({
    sNombre: { type: String, required: true },
    sApellidoPaterno: { type: String, required: true},
    sApellidoMaterno: { type: String, required: true},
    sUsuario: { type: String, required: true },
    sPassword: { type: String, required: true },
    iRol: { type: Number, required: true },
    aEsquemas: [{ type: mongoose.Schema.Types.ObjectId, ref: "Esquema", default: [] }],
});


UsuarioSchema.pre('save', async function (next) {
    if (!this.isModified('sPassword')) {
        return next();
    }
    this.sPassword = await bcrypt.hash(this.sPassword, 8);
    next();
});

export default mongoose.model('Usuario', UsuarioSchema);
