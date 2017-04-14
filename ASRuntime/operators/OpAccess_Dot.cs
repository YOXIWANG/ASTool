﻿using ASBinCode;
using ASBinCode.rtData;
using ASBinCode.rtti;
using System;
using System.Collections.Generic;
using System.Text;

namespace ASRuntime.operators
{
    class OpAccess_Dot
    {
        public static void exec_dot(Player player, StackFrame frame, OpStep step, IRunTimeScope scope)
        {
            IRunTimeValue obj = step.arg1.getValue(scope);

            if (rtNull.nullptr.Equals(obj))
            {
                frame.throwError(
                    new error.InternalError(
                        step.token, "Cannot access a property or method of a null object reference.",
                            new ASBinCode.rtData.rtString("Cannot access a property or method of a null object reference.")
                        ));
                
            }
            else
            {
                rtObject rtObj =
                    (rtObject)obj;
                
                StackSlot slot = step.reg.getISlot(scope) as StackSlot;
                if (slot != null)
                {
                    
                    ISLOT lintoslot = ((ILeftValue)step.arg2).getISlot(rtObj.objScope);
                    if (lintoslot == null)
                    {
                        frame.throwError((new error.InternalError(step.token,
                            "没有获取到类成员数据"
                            )));
                    }
                    if (lintoslot is ClassPropertyGetter.PropertySlot)
                    {
                        slot.propGetSet = (ClassPropertyGetter)step.arg2;
                        slot.propBindObj = rtObj;
                    }
                    

                    slot.linkTo(lintoslot);
                    
                }
                else
                {
                    frame.throwError((new error.InternalError(step.token,
                         "dot操作结果必然是一个StackSlot"
                         )));
                }

            }

            frame.endStep(step);
        }

        public static void exec_method(Player player, StackFrame frame, OpStep step, IRunTimeScope scope)
        {
            IRunTimeValue obj = step.arg1.getValue(scope);

            if (rtNull.nullptr.Equals(obj))
            {
                frame.throwError(
                    new error.InternalError(
                        step.token, "Cannot access a property or method of a null object reference.",
                            new ASBinCode.rtData.rtString("Cannot access a property or method of a null object reference.")
                        ));

            }
            else
            {
                rtObject rtObj =
                    (rtObject)obj;

                StackSlot slot = step.reg.getISlot(scope) as StackSlot;
                if (slot != null)
                {
                    ISLOT lintoslot = ((ClassMethodGetter)step.arg2).getISlot(rtObj.objScope);
                    if (lintoslot == null)
                    {
                        frame.throwError((new error.InternalError(step.token,
                         "没有获取到类成员数据"
                         )));
                    }

                    slot.linkTo(lintoslot);
                }
                else
                {
                    frame.throwError((new error.InternalError(step.token,
                         "dot操作结果必然是一个StackSlot"
                         )));
                }

            }

            frame.endStep(step);
        }

        public static void exec_dot_byname(Player player, StackFrame frame, OpStep step, IRunTimeScope scope)
        {
            IRunTimeValue obj = step.arg1.getValue(scope);
            if (rtNull.nullptr.Equals(obj))
            {
                frame.throwError(
                    new error.InternalError(
                        step.token, "Cannot access a property or method of a null object reference.",
                        new rtString("Cannot access a property or method of a null object reference."
                        )
                        )
                        );
                frame.endStep(step);
            }
            else
            {
                if (!(obj is rtObject))
                {
                    var objtype = obj.rtType;
                    if (objtype < RunTimeDataType.unknown
                        &&
                        player.swc.primitive_to_class_table[objtype] != null
                        )
                    {
                        //***转换为对象***

                        OpCast.Primitive_to_Object(obj, frame, step.token, scope, frame._tempSlot1,
                            step, _primitive_toObj);

                        return;
                    }
                    else
                    {
                        frame.throwOpException(step.token, step.opCode);
                        frame.endStep();
                        return;
                    }
                }

                rtObject rtObj = (rtObject)obj;
                //string name = ((rtString)step.arg2.getValue(scope)).value;
                _loadname_dot(rtObj, step, frame, scope, player);
            }

        }

        private static void _loadname_dot(rtObject rtObj,OpStep step,StackFrame frame,IRunTimeScope scope,Player player)
        {
            var v2 = step.arg2.getValue(scope);
            if (v2.rtType == RunTimeDataType.rt_string)
            {
                _exec_dot_name(rtObj, ((rtString)v2).value, step, frame, scope, player);
            }
            else
            {
                BlockCallBackBase cb = new BlockCallBackBase();
                cb.setCallBacker(_convert_callbacker);
                cb.scope = scope;
                cb.step = step;

                object[] args = new object[5];
                args[0] = rtObj;
                args[1] = frame;
                args[2] = player;

                cb.args = args;

                //**获取name
                OpCast.CastValue(step.arg2.getValue(scope), RunTimeDataType.rt_string, frame, step.token, scope
                    ,
                    frame._tempSlot1, cb,false
                    );

            }
        }


        private static void _convert_callbacker(BlockCallBackBase sender,object args)
        {
            object[] a = (object[])sender.args;

            StackFrame frame = (StackFrame)a[1];
            var nv = TypeConverter.ConvertToString( frame._tempSlot1.getValue(),frame,sender.step.token);

            _exec_dot_name((rtObject)a[0], nv, sender.step, frame, sender.scope, (Player)a[2]);

        }

        /// <summary>
        /// 从原型链中查找对象
        /// </summary>
        /// <returns></returns>
        public static DynamicObject findInProtoType(DynamicObject dobj,string name,StackFrame frame,SourceToken token,out bool haserror)
        {
            haserror = false;
            //***从原型链中查找对象***
            while (dobj != null && !dobj.hasproperty(name))
            {
                if (dobj._prototype_ != null
                    //&&
                    //dobj._prototype_.value is DynamicObject
                    )
                {
                    var protoObj = dobj._prototype_;
                    //****_prototype_的类型，只可能是Function对象或Class对象 Class对象尚未实现
                    if (protoObj._class.classid == 8) //Function 
                    {
                        dobj = (DynamicObject)((rtObject)protoObj.memberData[1].getValue()).value;
                    }
                    else if (protoObj._class.classid == 1) //搜索到根Object
                    {
                        dobj = null;
                    }
                    else
                    {
                        haserror = true;
                        frame.throwError((new error.InternalError(token,
                             "遭遇了异常的_prototype_"
                             )));
                        break;
                    }
                }
                else
                {
                    dobj = null;
                }
            }

            if (dobj == null || haserror)
            {
                return null;
            }
            else
            {
                return dobj;
            }
        }


        private static void _exec_dot_name(rtObject rtObj,string name,OpStep step,StackFrame frame,IRunTimeScope scope,Player player)
        {
            do
            {

                if (rtObj.value is Global_Object)
                {
                    Global_Object gobj = (Global_Object)rtObj.value;

                    if (!gobj.hasproperty(name))//propSlot == null)
                    {
                        frame.throwError(
                        new error.InternalError(
                           step.token, rtObj.ToString() + "找不到" + name,
                           new rtString(rtObj.ToString() + "找不到" + name)
                           ));

                        break;
                    }
                    else
                    {
                        ISLOT propSlot = gobj[name];
                        StackSlot slot = step.reg.getISlot(scope) as StackSlot;
                        if (slot != null)
                        {
                            slot.linkTo(propSlot);
                        }
                        else
                        {
                            frame.throwError((new error.InternalError(step.token,
                                 "dot操作结果必然是一个StackSlot"
                                 )));
                        }
                    }
                }
                else
                {
                    //*****检查是否是数组*****
                    RunTimeDataType ot;
                    if (TypeConverter.Object_CanImplicit_ToPrimitive(rtObj.rtType, player.swc, out ot))
                    {
                        if (ot == RunTimeDataType.rt_array)
                        {
                            IRunTimeValue obj = TypeConverter.ObjectImplicit_ToPrimitive(rtObj);
                            if (_assess_array(obj, frame, step, scope))
                            {
                                return;
                            }
                        }
                    }
                    


                    //***从对象中查找***
                    CodeBlock block = player.swc.blocks[scope.blockId];
                    Class finder;
                    if (block.isoutclass)
                    {
                        finder = null;
                    }
                    else
                    {
                        finder = player.swc.classes[block.define_class_id];
                    }

                    var member = ClassMemberFinder.find(rtObj.value._class, name, finder);

                    if (member == null)
                    {
                        if (rtObj.value._class.dynamic) //如果是动态类型
                        {
                            DynamicObject dobj = (DynamicObject)rtObj.value;

                            bool haserror;
                            dobj = findInProtoType(dobj, name, frame, step.token, out haserror);
                            if (haserror)
                            {
                                break;
                            }
                            
                            StackSlot dslot = step.reg.getISlot(scope) as StackSlot;
                            if (dslot != null)
                            {
                                if (dobj == null || !dobj.hasproperty(name))
                                {
                                    //原型链中也不存在对象
                                    dobj = (DynamicObject)rtObj.value;
                                    DynamicPropertySlot heapslot = new DynamicPropertySlot(rtObj, true);
                                    heapslot._propname = name;
                                    heapslot.directSet(rtUndefined.undefined);
                                    dobj.createproperty(name, heapslot);
                                }

                                ISLOT v = dobj[name];
                                if( !ReferenceEquals(dobj,rtObj.value) )
                                {
                                    if (v.getValue().rtType == RunTimeDataType.rt_function)
                                    {
                                        ObjectMemberSlot tempslot = new ObjectMemberSlot(rtObj);
                                        tempslot.directSet(v.getValue());
                                        v = tempslot;
                                    }
                                    else if (v.getValue().rtType > RunTimeDataType.unknown)
                                    {
                                        RunTimeDataType tout;
                                        if (TypeConverter.Object_CanImplicit_ToPrimitive(v.getValue().rtType,
                                            player.swc, out tout))
                                        {
                                            if (tout == RunTimeDataType.rt_function)
                                            {
                                                ObjectMemberSlot tempslot = new ObjectMemberSlot(rtObj);
                                                tempslot.directSet(
                                                    TypeConverter.ObjectImplicit_ToPrimitive(
                                                    (rtObject)v.getValue()));

                                                v = tempslot;
                                            }
                                        }
                                    }

                                    prototypeSlot pslot = new prototypeSlot(rtObj, name, v);
                                    v = pslot;
                                    //dslot._protoRootObj = (DynamicObject)rtObj.value;
                                    //dslot._protoname = name;
                                }

                                dslot.linkTo(v);
                            }
                            else
                            {
                                frame.throwError((new error.InternalError(step.token,
                                     "dot操作结果必然是一个StackSlot"
                                     )));
                            }


                            break;
                        }
                    }

                    if (member == null)
                    {
                        frame.throwError(
                        new error.InternalError(
                           step.token, rtObj.ToString() + "找不到" + name,
                           new rtString(rtObj.ToString() + "找不到" + name)
                           ));

                        break;
                    }

                    if (member.isConstructor)
                    {
                        frame.throwError(
                        new error.InternalError(
                           step.token, rtObj.ToString() + "找不到" + name,
                           new rtString(rtObj.ToString() + "找不到" + name)
                           ));

                        break;
                    }

                    {
                        StackSlot slot = step.reg.getISlot(scope) as StackSlot;
                        if (slot != null)
                        {
                            ISLOT linkto = ((ILeftValue)member.bindField).getISlot(rtObj.objScope);
                            slot.linkTo(linkto);
                            if (linkto is ClassPropertyGetter.PropertySlot)
                            {
                                slot.propBindObj = rtObj;
                                slot.propGetSet = (ClassPropertyGetter)member.bindField;
                            }
                        }
                        else
                        {
                            frame.throwError((new error.InternalError(step.token,
                                 "dot操作结果必然是一个StackSlot"
                                 )));
                        }
                    }

                }

            }
            while (false);

            frame.endStep(step);
        }




        /// <summary>
        /// []访问。需要动态检查是否是数组
        /// </summary>
        /// <param name="player"></param>
        /// <param name="frame"></param>
        /// <param name="step"></param>
        /// <param name="scope"></param>
        public static void exec_bracket_access(Player player, StackFrame frame, OpStep step, IRunTimeScope scope)
        {
            IRunTimeValue obj = step.arg1.getValue(scope);
            if (rtNull.nullptr.Equals(obj))
            {
                frame.throwError(
                    new error.InternalError(
                        step.token, "Cannot access a property or method of a null object reference.",
                        new rtString("Cannot access a property or method of a null object reference."
                        )
                        )
                        );

            }
            else
            {
                if (obj is rtArray)
                {
                    if (_assess_array(obj, frame, step, scope))
                    {
                        return;
                    }
                }
                else if (obj is rtObject)
                {
                    if (player.swc.dict_Vector_type.ContainsKey(((rtObject)obj).value._class))
                    {
                        //说明是Vector数组
                        OpVector.exec_AccessorBind_ConvertIdx(player, frame, step, scope);
                        return;
                    }
                }
                

                if (!(obj is rtObject))
                {
                    var objtype = obj.rtType;
                    if (objtype < RunTimeDataType.unknown
                        &&
                        player.swc.primitive_to_class_table[objtype] != null
                        )
                    {
                        //***转换为对象***

                        OpCast.Primitive_to_Object(obj, frame, step.token, scope, frame._tempSlot1,
                            step, _primitive_toObj);

                        return;
                    }
                    else if (obj == rtUndefined.undefined)
                    {
                        frame.throwError(new error.InternalError(step.token,
                            "A term is undefined and has no properties",
                            new rtString("A term is undefined and has no properties")
                            ));
                        frame.endStep();
                        return;
                    }
                    else
                    {
                        frame.throwOpException(step.token, step.opCode);
                        frame.endStep();
                        return;
                    }
                }
                _primitive_toObj(obj, null, frame, step, scope);
            }

        }

        private static void _primitive_toObj(ASBinCode.IRunTimeValue v1, ASBinCode.IRunTimeValue v_temp, StackFrame frame, ASBinCode.OpStep step, ASBinCode.IRunTimeScope scope)
        {
            if (v1.rtType < RunTimeDataType.unknown)
            {
                frame.throwError(
                    new error.InternalError(
                    step.token, "基本数据类型没有转换为引用类型",
                    new rtString("基本数据类型没有转换为引用类型"
                    )
                    )
                    );
                return;
            }

            rtObject rtObj = (rtObject)v1;

            if (true) //****检查如果不是数组
            {
                _loadname_dot(rtObj, step, frame, scope, frame.player);
            }
            else
            {
                frame.throwError(
                    new error.InternalError(
                    step.token, "数值未实现",
                    new rtString("数组未实现"
                    )
                    )
                    );
                frame.endStep();
            }
        }


        private static bool _assess_array( IRunTimeValue obj, StackFrame frame, OpStep step,IRunTimeScope scope)
        {
            var idx = step.arg2.getValue(scope);
            var number = TypeConverter.ConvertToNumber(idx, frame, step.token);
            if (!(double.IsNaN(number) || double.IsInfinity(number)))
            {
                if ((int)number >= 0)
                {
                    if ((int)number >= ((rtArray)obj).innerArray.Count)
                    {
                        var toadd = ((rtArray)obj).innerArray;
                        int addnum = (int)number+1 - toadd.Count;

                        while (addnum>0)
                        {
                            addnum--;
                            toadd.Add(rtUndefined.undefined);
                        }
                    }


                    //***访问数组的内容***
                    StackSlot stackSlot= (StackSlot)step.reg.getISlot(scope);
                    //stackSlot.fromArray = (rtArray)obj;
                    //stackSlot.fromArrayIndex = (int)number;

                    //step.reg.getISlot(scope).directSet(((rtArray)obj).innerArray[(int)number]);
                    stackSlot.linkTo(new arraySlot((rtArray)obj, (int)number));

                    frame.endStep();
                    return true;
                }
            }
            return false;
        }

        internal class arraySlot : ISLOT
        {
            rtArray array;
            int idx;
            public arraySlot(rtArray array,int idx)
            {
                this.array = array;
                this.idx = idx;
            }

            public bool isPropGetterSetter
            {
                get
                {
                    return false;
                }
            }

            public void clear()
            {
                //throw new NotImplementedException();
            }

            public bool directSet(IRunTimeValue value)
            {
                array.innerArray[idx] = (IRunTimeValue)value.Clone(); //对数组的直接赋值，需要Clone
                return true;
                //throw new NotImplementedException();
            }

            public IRunTimeValue getValue()
            {
                return array.innerArray[idx];
                //throw new NotImplementedException();
            }

            public void setValue(rtUndefined value)
            {
                throw new NotImplementedException();
            }

            public void setValue(rtNull value)
            {
                throw new NotImplementedException();
            }

            public void setValue(int value)
            {
                throw new NotImplementedException();
            }

            public void setValue(string value)
            {
                throw new NotImplementedException();
            }

            public void setValue(uint value)
            {
                throw new NotImplementedException();
            }

            public void setValue(double value)
            {
                throw new NotImplementedException();
            }

            public void setValue(rtBoolean value)
            {
                throw new NotImplementedException();
            }
        }


        class prototypeSlot : ISLOT
        {
            internal rtObject _protoRootObj;
            internal string _protoname;

            private ISLOT findSlot;

            public prototypeSlot(rtObject _protoRootObj, string _protoname,
                ISLOT findSlot
                )
            {
                this._protoRootObj = _protoRootObj;
                this._protoname = _protoname;
                this.findSlot = findSlot;
            }


            public bool isPropGetterSetter
            {
                get
                {
                    return false;
                }
            }

            public void clear()
            {
                //throw new NotImplementedException();
            }

            public bool directSet(IRunTimeValue value)
            {
                //throw new NotImplementedException();
                DynamicPropertySlot heapslot = new DynamicPropertySlot(_protoRootObj, true);
                heapslot._propname = _protoname;
                heapslot.directSet(value);
                ((DynamicObject)_protoRootObj.value).createproperty(_protoname, heapslot);

                return true;
            }

            public IRunTimeValue getValue()
            {
                return findSlot.getValue();
                //throw new NotImplementedException();
            }

            public void setValue(rtUndefined value)
            {
                throw new NotImplementedException();
            }

            public void setValue(rtNull value)
            {
                throw new NotImplementedException();
            }

            public void setValue(int value)
            {
                throw new NotImplementedException();
            }

            public void setValue(string value)
            {
                throw new NotImplementedException();
            }

            public void setValue(uint value)
            {
                throw new NotImplementedException();
            }

            public void setValue(double value)
            {
                throw new NotImplementedException();
            }

            public void setValue(rtBoolean value)
            {
                throw new NotImplementedException();
            }
        }

    }
}
